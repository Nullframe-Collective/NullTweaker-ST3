using UnityEngine;

namespace CustomPlayerCustomization
{
    public struct CustomizationTransform
    {
        public Vector3 Position { get; set; }
        public Vector3 LocalPosition { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Scale { get; set; }

        public CustomizationTransform()
        {
            Position = Vector3.zero;
            LocalPosition = Vector3.zero;
            Rotation = Vector3.zero;
            Scale = Vector3.one;
        }

        public CustomizationTransform(Vector3 position, Vector3 localPosition, Vector3 rotation, Vector3 scale)
        {
            Position = position;
            LocalPosition = localPosition;
            Rotation = rotation;
            Scale = scale;
        }
    }
}
