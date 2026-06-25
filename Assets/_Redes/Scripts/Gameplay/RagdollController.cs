using UnityEngine;
using System.Collections.Generic;

namespace Redes.Gameplay
{
    /// <summary>
    /// Procedurally registers bones on Start, adds capsule/sphere colliders and rigidbodies,
    /// and provides a simple API to enable/disable Ragdoll physics.
    /// Works for both online (NetworkPlayer) and offline (OfflinePlayerTester / DummyEnemy) characters.
    /// </summary>
    public class RagdollController : MonoBehaviour
    {
        private Animator _animator;
        private Rigidbody _mainRigidbody;
        private Collider _mainCollider;

        private struct BoneJointData
        {
            public Transform transform;
            public Rigidbody rigidbody;
            public Collider collider;
            public Vector3 initialLocalPos;
            public Quaternion initialLocalRot;
        }

        private List<BoneJointData> _bones = new List<BoneJointData>();
        private bool _isRagdollActive = false;

        private void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            _mainRigidbody = GetComponent<Rigidbody>();
            _mainCollider = GetComponent<Collider>();

            SetupRagdollBones();
            SetRagdollActive(false);
        }

        private void SetupRagdollBones()
        {
            // Find all transforms in the model
            var allTransforms = GetComponentsInChildren<Transform>();
            foreach (var t in allTransforms)
            {
                if (t == transform) continue;

                string name = t.name.ToLower();
                // We identify key bones based on name match
                bool isKeyBone = false;
                float mass = 1f;
                float radius = 0.15f;
                float height = 0.3f;
                int direction = 1; // Y-axis

                if (name.Contains("pelvis") || name.Contains("hips"))
                {
                    isKeyBone = true;
                    mass = 15f;
                    radius = 0.25f;
                }
                else if (name.Contains("spine") || name.Contains("chest"))
                {
                    isKeyBone = true;
                    mass = 20f;
                    radius = 0.22f;
                    height = 0.4f;
                }
                else if (name.Contains("head"))
                {
                    isKeyBone = true;
                    mass = 10f;
                    radius = 0.18f;
                }
                else if (name.Contains("thigh") || name.Contains("upperleg"))
                {
                    isKeyBone = true;
                    mass = 10f;
                    radius = 0.12f;
                    height = 0.45f;
                }
                else if (name.Contains("calf") || name.Contains("lowerleg") || name.Contains("shin"))
                {
                    isKeyBone = true;
                    mass = 8f;
                    radius = 0.1f;
                    height = 0.4f;
                }
                else if (name.Contains("upperarm"))
                {
                    isKeyBone = true;
                    mass = 7f;
                    radius = 0.09f;
                    height = 0.35f;
                    direction = 2; // Z or X depending on skeleton alignment
                }
                else if (name.Contains("forearm") || name.Contains("lowerarm"))
                {
                    isKeyBone = true;
                    mass = 5f;
                    radius = 0.08f;
                    height = 0.3f;
                    direction = 2;
                }

                if (isKeyBone)
                {
                    // Add Rigidbody if not present
                    var rb = t.GetComponent<Rigidbody>();
                    if (rb == null) rb = t.gameObject.AddComponent<Rigidbody>();
                    rb.mass = mass;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;

                    // Add Collider if not present
                    Collider col = t.GetComponent<Collider>();
                    if (col == null)
                    {
                        if (name.Contains("pelvis") || name.Contains("hips") || name.Contains("head"))
                        {
                            var sphere = t.gameObject.AddComponent<SphereCollider>();
                            sphere.radius = radius;
                            col = sphere;
                        }
                        else
                        {
                            var capsule = t.gameObject.AddComponent<CapsuleCollider>();
                            capsule.radius = radius;
                            capsule.height = height;
                            capsule.direction = direction;
                            col = capsule;
                        }
                    }

                    // Add CharacterJoint if it has a parent bone with a Rigidbody
                    if (t.parent != null && !name.Contains("pelvis") && !name.Contains("hips"))
                    {
                        var parentRb = t.parent.GetComponentInParent<Rigidbody>();
                        if (parentRb != null && parentRb.gameObject != gameObject)
                        {
                            var joint = t.GetComponent<CharacterJoint>();
                            if (joint == null) joint = t.gameObject.AddComponent<CharacterJoint>();
                            joint.connectedBody = parentRb;
                        }
                    }

                    _bones.Add(new BoneJointData
                    {
                        transform = t,
                        rigidbody = rb,
                        collider = col,
                        initialLocalPos = t.localPosition,
                        initialLocalRot = t.localRotation
                    });
                }
            }
        }

        public void SetRagdollActive(bool active)
        {
            _isRagdollActive = active;

            if (_animator != null)
            {
                _animator.enabled = !active;
            }

            if (_mainRigidbody != null)
            {
                _mainRigidbody.isKinematic = active;
            }

            if (_mainCollider != null)
            {
                _mainCollider.enabled = !active;
            }

            foreach (var bone in _bones)
            {
                if (bone.rigidbody != null)
                {
                    bone.rigidbody.isKinematic = !active;
                    bone.rigidbody.useGravity = active;
                }
                if (bone.collider != null)
                {
                    bone.collider.isTrigger = !active;
                }
            }
        }

        public void ResetBones()
        {
            SetRagdollActive(false);
            foreach (var bone in _bones)
            {
                bone.transform.localPosition = bone.initialLocalPos;
                bone.transform.localRotation = bone.initialLocalRot;
                if (bone.rigidbody != null)
                {
                    bone.rigidbody.linearVelocity = Vector3.zero;
                    bone.rigidbody.angularVelocity = Vector3.zero;
                }
            }
        }
    }
}
