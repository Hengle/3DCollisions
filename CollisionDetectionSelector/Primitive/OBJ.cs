﻿using OpenTK.Graphics.OpenGL;
using Math_Implementation;
using System.Collections.Generic;

namespace CollisionDetectionSelector.Primitive {
    class OBJ {
        
        #region Node
        public OBJ Parent = null;
        public List<OBJ> Children = new List<OBJ>();
        #endregion
        
        OBJLoader model = null;

        //expose model bvhRoot
        public BVHNode BVHRoot {
            get {
                return model.BvhRoot;
            }
        }
        //end
        public bool IsEmpty {
            get {
                return model == null;
            }
        }

        protected Vector3 position = new Vector3(0.0f, 0.0f, 0.0f);
        protected Vector3 rotation = new Vector3(0.0f, 0.0f, 0.0f);
        protected Vector3 scale = new Vector3(1.0f, 1.0f, 1.0f);

        protected Matrix4 worldMatrix;
        protected bool wasRendered = false;
        protected bool dirtySelf = true;//true by default
        protected bool dirty {
            get {
                return dirtySelf;
            }
            set {
                bool contanier = value;
                dirtySelf = contanier;

                System.Diagnostics.Debug.Assert(dirtySelf == value);
                if (dirtySelf) {
                    foreach (OBJ child in Children) {
                        child.dirty = true;
                    }
                }
            }
        }

        #region WorldSpace
        public Matrix4 WorldMatrix {
            get {
                if (dirty) {
                    Matrix4 translation = Matrix4.Translate(position);

                    Matrix4 pitch = Matrix4.XRotation(rotation.X);
                    Matrix4 yaw = Matrix4.YRotation(rotation.Y);
                    Matrix4 roll = Matrix4.ZRotation(rotation.Z);
                    Matrix4 orientation = roll * pitch * yaw;

                    Matrix4 scaling = Matrix4.Scale(scale);
                    worldMatrix = translation * orientation * scaling;

                    if (Parent != null) {
                        worldMatrix = Parent.WorldMatrix* worldMatrix ;
                    }

                    dirty = false;
                }
                return worldMatrix;
            }
        }
        #endregion

        #region LocalSpace
        public Vector3 Position {
            get { return position; }
            set {
                position = value;
                dirty = true;
            }
        }

        public Vector3 Rotation {
            get { return rotation; }
            set {
                rotation = value;
                dirty = true;
            }
        }

        public Vector3 Scale {
            get { return scale; }
            set {
                scale = value;
                dirty = true;
            }
        }
        #endregion

        public AABB BoundingBox {
            get {
                return model.BoundingBox;
            }
        }
        public Sphere BoundingSphere {
            get {
                return model.BoundingSphere;
            }
        }
        public Triangle[] Mesh {
            get {
                return model.CollisionMesh;
            }
        }
        public OBJ(OBJLoader loader) {
            model = loader;
        }

        private int ChildrenRender(bool normal,bool bvh, bool debug) {
            int result = 0;
            if (Children != null) {
                foreach(OBJ child in Children) {
                    if (normal) {
                        result += child.Render();
                    }
                    else if (bvh) {
                        child.RenderBVH();
                    }
                    else if (debug) {
                        child.DebugRender();
                    }
                    if(child.Children != null) {
                        result += child.ChildrenRender(normal, bvh, debug);
                    }
                }
            }
            return result;
        }
        public int Render() {
            int result = 0;

            GL.PushMatrix();
            //always getter
            GL.MultMatrix(WorldMatrix.OpenGL);
            if (model != null) {
                model.Render();
                result++;
            }
            GL.PopMatrix();
            
            result += ChildrenRender(true, false, false);
            return result;
        }

        public void DebugRender() {
            GL.PushMatrix();
            GL.MultMatrix(WorldMatrix.OpenGL);
            if (model != null) {
                model.DebugRender();
            }
            GL.PopMatrix();
            ChildrenRender(false, false, true);
        }

        #region BVH
        public void RenderBVH() {
            GL.PushMatrix();
            GL.MultMatrix(WorldMatrix.OpenGL);
            if (model != null) {
                model.RenderBVH();
            }
            GL.PopMatrix();
            ChildrenRender(false, true, false);
        }
        #endregion

        public override string ToString() {
            return "Triangle count: " + model.NumCollisionTriangles;
        }
        public void ResetRenderFlag() {
            wasRendered = false;
        }
        public bool NonRecursiveRender(Plane[] frustum) {
            if (model == null || wasRendered) {
                return false;
            }
            Sphere bounds = new Sphere();
            bounds.Position = new Point(Matrix4.MultiplyPoint(WorldMatrix, model.BoundingSphere.vPosition));
            float scalar = System.Math.Abs(System.Math.Max(System.Math.Max(
                                                                            System.Math.Abs(WorldMatrix[0, 0]), System.Math.Abs(WorldMatrix[1, 1])),
                                                                            System.Math.Abs(WorldMatrix[2, 2])));
            bounds.Radius = model.BoundingSphere.Radius * scalar;
            if (!Collisions.Intersects(frustum, bounds)) {
                return false;
            }

            wasRendered = true;
            GL.PushMatrix();
            GL.MultMatrix(WorldMatrix.OpenGL);
            model.Render();
            GL.PopMatrix();
            return true;
        }
    }
}
