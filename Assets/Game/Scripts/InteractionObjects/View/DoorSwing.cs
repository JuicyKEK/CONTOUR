using UnityEngine;

namespace Game.Scripts.InteractionObjects
{
    /// <summary>
    /// Геометрия створки: в какую сторону она уходит при открытии с разбега (IInteractionDoor.TryRushOpen), как открыть
    /// её в обратную, где у неё лицевая сторона. Общая для дверей и их видов.
    /// </summary>
    public static class DoorSwing
    {
        /// <summary>
        /// Нормаль створки в мировых координатах: самая тонкая ось BoxCollider на объекте петли (нет его - ось Z петли).
        /// </summary>
        public static Vector3 GetLeafNormal(Transform hinge)
        {
            if (!hinge.TryGetComponent(out BoxCollider box))
            {
                return hinge.forward;
            }

            Vector3 size = Vector3.Scale(box.size, hinge.lossyScale);
            size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));

            if (size.x <= size.y && size.x <= size.z)
            {
                return hinge.right;
            }

            return size.y <= size.z ? hinge.up : hinge.forward;
        }

        /// <summary>
        /// Тот же поворот от закрытого положения, что closed -> open, но в обратную сторону.
        /// </summary>
        public static Quaternion Reverse(Quaternion closedLocalRotation, Quaternion openLocalRotation)
        {
            Quaternion swing = Quaternion.Inverse(closedLocalRotation) * openLocalRotation;
            return closedLocalRotation * Quaternion.Inverse(swing);
        }

        /// <summary>
        /// Уходит ли створка при открытии closed -> open в ту сторону, где точка point. Створка - коллайдер на объекте
        /// петли (нет коллайдера - её рендереры); учитывается только сдвиг середины створки поперёк закрытой створки.
        /// </summary>
        public static bool IsSwingTowards(Transform hinge, Quaternion closedLocalRotation, Quaternion openLocalRotation,
            Vector3 point)
        {
            if (!TryGetLeafCenter(hinge, out Vector3 localCenter))
            {
                return false;
            }

            Quaternion parentRotation = hinge.parent != null ? hinge.parent.rotation : Quaternion.identity;
            Vector3 scaledCenter = Vector3.Scale(hinge.localScale, localCenter);
            Vector3 closedOffset = parentRotation * (closedLocalRotation * scaledCenter);
            Vector3 openOffset = parentRotation * (openLocalRotation * scaledCenter);

            Vector3 along = new Vector3(closedOffset.x, 0f, closedOffset.z);

            if (along.sqrMagnitude < 1e-6f)
            {
                return false;
            }

            along.Normalize();

            Vector3 swing = openOffset - closedOffset;
            swing.y = 0f;
            swing -= Vector3.Dot(swing, along) * along;

            Vector3 toPoint = point - (hinge.position + closedOffset);
            toPoint.y = 0f;

            return Vector3.Dot(swing, toPoint) > 0f;
        }

        /// <summary>
        /// Середина створки в локальных координатах петли. Берётся из настроек коллайдера, а не из его bounds:
        /// пока створка поворачивается, коллайдер выключен и bounds пустые.
        /// </summary>
        private static bool TryGetLeafCenter(Transform hinge, out Vector3 localCenter)
        {
            switch (hinge.GetComponent<Collider>())
            {
                case BoxCollider box:
                    localCenter = box.center;
                    return true;
                case CapsuleCollider capsule:
                    localCenter = capsule.center;
                    return true;
                case SphereCollider sphere:
                    localCenter = sphere.center;
                    return true;
                case MeshCollider mesh when mesh.sharedMesh != null:
                    localCenter = mesh.sharedMesh.bounds.center;
                    return true;
            }

            Renderer[] renderers = hinge.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
            {
                localCenter = default;
                return false;
            }

            Bounds bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            localCenter = hinge.InverseTransformPoint(bounds.center);
            return true;
        }
    }
}
