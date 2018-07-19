using System;
using System.Collections.Generic;
using System.Numerics;
using System.Diagnostics;

namespace Glitch
{
    public class Transform : Component
    {
        private Vector3 _localPosition;
        private Quaternion _localRotation = Quaternion.Identity;
        private Vector3 _localScale = Vector3.One;
        private Transform _parent;
        private readonly List<Transform> _children = new List<Transform>(0);


        public delegate void ParentChangedHandler(Transform t, Transform oldParent, Transform newParent);
        public event ParentChangedHandler ParentChanged;



        public Vector3 Position
        {
            get
            {
                Vector3 pos = _localPosition;
                if (Parent != null)
                {
                    pos = Vector3.Transform(pos, Parent.GetWorldMatrix());
                }

                return pos;
            }
            set
            {
                Vector3 oldPosition = Position;

                Vector3 parentPos = Parent != null ? Parent.Position : Vector3.Zero;
                _localPosition = value - parentPos;

                OnPositionManuallyChanged(oldPosition);
                OnPositionChanged();
            }
        }

        public Vector3 LocalPosition
        {
            get
            {
                return _localPosition;
            }
            set
            {
                Vector3 oldPosition = Position;
                if (value != oldPosition)
                {
                    _localPosition = value;

                    OnPositionChanged();
                    OnPositionManuallyChanged(oldPosition);
                }
            }
        }

        public event Action<Vector3, Vector3> PositionManuallyChanged;
        private void OnPositionManuallyChanged(Vector3 oldPosition)
        {
            PositionManuallyChanged?.Invoke(oldPosition, Position);
        }

        public event Action<Transform> TransformChanged;
        public event Action<Vector3> PositionChanged;
        private void OnPositionChanged()
        {
            PositionChanged?.Invoke(Position);
            TransformChanged?.Invoke(this);
        }

        public Quaternion Rotation
        {
            get
            {
                Quaternion rot = _localRotation;
                if (Parent != null)
                {
                    rot = Quaternion.Concatenate(Parent.Rotation, rot);
                }

                return rot;
            }
            set
            {
                if (value != Rotation)
                {
                    Quaternion oldRotation = Rotation;

                    Quaternion parentRot = Parent != null ? Parent.Rotation : Quaternion.Identity;
                    _localRotation = Quaternion.Concatenate(Quaternion.Inverse(parentRot), value);

                    OnRotationManuallyChanged(oldRotation);
                    OnRotationChanged();
                }
            }
        }

        public Quaternion LocalRotation
        {
            get
            {
                return _localRotation;
            }
            set
            {
                Quaternion oldRotation = Rotation;
                _localRotation = value;
                OnRotationManuallyChanged(oldRotation);
                OnRotationChanged();
            }
        }

        public event Action<Quaternion, Quaternion> RotationManuallyChanged;
        private void OnRotationManuallyChanged(Quaternion oldRotation)
        {
            RotationManuallyChanged?.Invoke(oldRotation, Rotation);
        }

        public event Action<Quaternion> RotationChanged;
        private void OnRotationChanged()
        {
            RotationChanged?.Invoke(Rotation);
            TransformChanged?.Invoke(this);
        }

        public Vector3 Scale
        {
            get
            {
                Vector3 scale = _localScale;
                if (Parent != null)
                {
                    scale *= Parent.Scale;
                }

                return scale;
            }
            set
            {
                Vector3 parentScale = Parent != null ? Parent.Scale : Vector3.One;
                _localScale = value / parentScale;
                OnScalechanged();
            }
        }

        public Vector3 LocalScale
        {
            get { return _localScale; }
            set
            {
                _localScale = value;
                OnScalechanged();
            }
        }

        internal event Action<Vector3> ScaleChanged;
        private void OnScalechanged()
        {
            ScaleChanged?.Invoke(Scale);
            TransformChanged?.Invoke(this);
        }

        public Transform Parent
        {
            get
            {
                return _parent;
            }
            set
            {
                if (value == this)
                {
                    throw new InvalidOperationException("Cannot set a Transform's parent to itself.");
                }

                SetParent(value);
            }
        }

        /// <summary>
        /// Called when this transform's parent changes.
        /// </summary>
        /// <param name="newParent">The new parent. May be null.</param>
        private void SetParent(Transform newParent)
        {
            var oldParent = _parent;
            if (oldParent != null)
            {
                oldParent._children.Remove(this);
                oldParent.TransformChanged -= OnParentTransformChanged;
                oldParent.PositionManuallyChanged -= OnParentPositionChanged;
                oldParent.RotationManuallyChanged -= OnParentRotationChanged;
            }

            _parent = newParent;
            if (newParent != null)
            {
                newParent._children.Add(this);
                newParent.TransformChanged += OnParentTransformChanged;
                newParent.PositionManuallyChanged += OnParentPositionChanged;
                newParent.RotationManuallyChanged += OnParentRotationChanged;
            }

            ParentChanged?.Invoke(this, oldParent, _parent);

            TransformChanged?.Invoke(this);
            OnPositionChanged();

        }

        private void OnParentTransformChanged(Transform obj)
        {
            TransformChanged?.Invoke(this);
        }

        private void OnParentPositionChanged(Vector3 oldPos, Vector3 newPos)
        {
            OnPositionChanged();
            OnPositionManuallyChanged(oldPos + _localPosition);
        }

        private void OnParentRotationChanged(Quaternion oldParentRot, Quaternion newParentRot)
        {
            Quaternion oldRot;
            oldRot = _localRotation;

            OnRotationChanged();
            OnRotationManuallyChanged(oldRot);
        }

        public IReadOnlyList<Transform> Children => _children;

        public Matrix4x4 GetWorldMatrix()
        {

            Matrix4x4 mat = Matrix4x4.CreateScale(_localScale)
                * Matrix4x4.CreateFromQuaternion(LocalRotation)
                * Matrix4x4.CreateTranslation(LocalPosition);

            if (Parent != null)
            {
                mat *= Parent.GetWorldMatrix();
            }
            
            return mat;
        }

        // public Matrix4x4 GetTransformMatrix()
        // {
        //     return Matrix4x4.CreateScale(_localScale)
        //         * Matrix4x4.CreateFromQuaternion(_localRotation)
        //         * Matrix4x4.CreateTranslation(LocalPosition);
        // }

        public Vector3 Forward
        {
            get
            {
                return Vector3.Transform(-Vector3.UnitZ, Rotation);
            }
        }

        public Vector3 Up
        {
            get
            {
                return Vector3.Transform(Vector3.UnitY, Rotation);
            }
        }

        public Vector3 Right
        {
            get
            {
                return Vector3.Transform(Vector3.UnitX, Rotation);
            }
        }



        protected override void Attached(SystemRegistry registry)
        {
        }

        protected override void Removed(SystemRegistry registry)
        {
            if (Parent != null)
            {
                bool result = Parent._children.Remove(this);
                Debug.Assert(result);
            }
        }

        protected override void OnEnabled()
        {
        }

        protected override void OnDisabled()
        {
        }

        public override string ToString()
        {
            return $"[Transform] {GameObject.ToString()}";
        }
    }
}