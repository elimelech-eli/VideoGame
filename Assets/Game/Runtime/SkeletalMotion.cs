using System.Collections.Generic;
using UnityEngine;

namespace VideoGame
{
    public sealed class SkeletalMotion : MonoBehaviour
    {
        Transform actor, leftThigh, rightThigh, leftShin, rightShin, arm, forearm, hand, leftArm, leftForearm, leftHand;
        readonly Dictionary<Transform, Quaternion> rest = new Dictionary<Transform, Quaternion>();
        Vector3 previous;
        float phase, blend;
        Transform Bone(string name)
        {
            foreach (var candidate in GetComponentsInChildren<Transform>(true))
                if (candidate.name.Replace('.', '_') == name) { rest[candidate] = candidate.localRotation; return candidate; }
            return null;
        }
        void Start()
        {
            actor = transform.parent.parent;
            leftThigh = Bone("thigh_L"); rightThigh = Bone("thigh_R");
            leftShin = Bone("shin_L"); rightShin = Bone("shin_R");
            arm = Bone("upper_arm_R"); forearm = Bone("forearm_R"); hand = Bone("hand_R");
            leftArm = Bone("upper_arm_L"); leftForearm = Bone("forearm_L"); leftHand = Bone("hand_L");
            previous = actor.position;
        }
        void Rotate(Transform bone, float angle) { if (bone != null) bone.localRotation = rest[bone] * Quaternion.Euler(angle, 0, 0); }
        void LateUpdate()
        {
            float speed = Vector3.Distance(actor.position, previous) / Mathf.Max(.001f, Time.deltaTime);
            previous = actor.position;
            blend = Mathf.MoveTowards(blend, Mathf.Clamp01(speed / 5), Time.deltaTime * 8);
            phase += Time.deltaTime * Mathf.Lerp(5, 11, blend);
            float stride = Mathf.Sin(phase) * blend;
            Rotate(leftThigh, stride * 28); Rotate(rightThigh, -stride * 28);
            Rotate(leftShin, Mathf.Max(0, -stride) * 32); Rotate(rightShin, Mathf.Max(0, stride) * 32);
            if (arm != null && forearm != null && hand != null)
            {
                arm.localRotation = rest[arm]; forearm.localRotation = rest[forearm];
                arm.rotation = Quaternion.FromToRotation(forearm.position - arm.position, actor.forward * .6f - Vector3.up * .35f) * arm.rotation;
                forearm.rotation = Quaternion.FromToRotation(hand.position - forearm.position, actor.forward) * forearm.rotation;
            }
            if (leftArm != null && leftForearm != null && leftHand != null)
            {
                leftArm.localRotation = rest[leftArm]; leftForearm.localRotation = rest[leftForearm];
                leftArm.rotation = Quaternion.FromToRotation(leftForearm.position - leftArm.position, actor.forward * .15f - Vector3.up * .75f) * leftArm.rotation;
                leftForearm.rotation = Quaternion.FromToRotation(leftHand.position - leftForearm.position, actor.forward * .25f - Vector3.up * .7f) * leftForearm.rotation;
            }
        }
    }
}
