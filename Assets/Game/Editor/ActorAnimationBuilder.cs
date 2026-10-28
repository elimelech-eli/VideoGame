using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VideoGame.Editor
{
    // Rig-specific authored keyframes, baked into ordinary Unity animation clips.
    public static class ActorAnimationBuilder
    {
        const string Folder = "Assets/Game/Resources/Animations/";
        public static readonly string[] Names = { "Idle", "RunForward", "RunBack", "RunLeft", "RunRight", "Fire", "Melee", "Cast", "Dash", "Hit", "Death" };
        [MenuItem("Nexus/Rebuild actor animation clips")]
        public static void Rebuild() => Build(true);
        public static void Build() => Build(false);
        static void Build(bool force) => BuildFor("Assets/Game/Resources/Detailed/Prefabs/Operative.prefab", Folder, force);
        public static void BuildFor(string sourcePath, string folder, bool force)
        {
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null) return;
            foreach (string name in Names)
            {
                string path = folder + name + ".anim";
                if (!force && File.Exists(path)) continue;
                var actor = new GameObject("Animation authoring rig");
                var visual = new GameObject("Visual"); visual.transform.SetParent(actor.transform);
                var model = Object.Instantiate(source, visual.transform);
                var rig = new Rig(model.transform);
                bool loop = name == "Idle" || name.StartsWith("Run");
                float duration = name == "Idle" ? 2 : name.StartsWith("Run") ? .65f : name == "Death" ? 1.25f : name == "Melee" ? .65f : name == "Cast" ? .8f : name == "Dash" ? .35f : name == "Hit" ? .25f : .18f;
                var clip = new AnimationClip { name = name, legacy = true, frameRate = 30, wrapMode = loop ? WrapMode.Loop : WrapMode.ClampForever };
                var curves = new Dictionary<Transform, AnimationCurve[]>();
                foreach (var bone in rig.Bones.Values) curves[bone] = new[] {new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve()};
                for (int frame = 0; frame <= 40; frame++)
                {
                    float t = frame / 40f;
                    rig.Restore(); rig.Pose(name, t);
                    foreach (var pair in curves)
                    {
                        var bone = pair.Key; var q = bone.localRotation; var p = bone.localPosition;
                        float[] values = {q.x,q.y,q.z,q.w,p.x,p.y,p.z};
                        for (int i = 0; i < 7; i++) pair.Value[i].AddKey(t * duration, values[i]);
                    }
                }
                string[] properties = {"localRotation.x","localRotation.y","localRotation.z","localRotation.w","localPosition.x","localPosition.y","localPosition.z"};
                foreach (var pair in curves)
                    for (int i = 0; i < 7; i++)
                    {
                        if (model.name.StartsWith("OperativeV03"))
                            for (int key = 0; key < pair.Value[i].length; key++)
                            {
                                AnimationUtility.SetKeyLeftTangentMode(pair.Value[i], key, AnimationUtility.TangentMode.ClampedAuto);
                                AnimationUtility.SetKeyRightTangentMode(pair.Value[i], key, AnimationUtility.TangentMode.ClampedAuto);
                            }
                        clip.SetCurve(AnimationUtility.CalculateTransformPath(pair.Key, model.transform), typeof(Transform), properties[i], pair.Value[i]);
                    }
                clip.EnsureQuaternionContinuity();
                var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (existing == null) AssetDatabase.CreateAsset(clip, path);
                else { EditorUtility.CopySerialized(clip, existing); Object.DestroyImmediate(clip); }
                Object.DestroyImmediate(actor);
            }
            AssetDatabase.SaveAssets();
        }
        sealed class Rig
        {
            public readonly Dictionary<string, Transform> Bones = new Dictionary<string, Transform>();
            readonly Dictionary<Transform, Quaternion> rotations = new Dictionary<Transform, Quaternion>();
            readonly Dictionary<Transform, Vector3> positions = new Dictionary<Transform, Vector3>();
            float height;
            readonly bool rifleOperator;
            readonly Transform weaponSocket;
            public Rig(Transform model)
            {
                rifleOperator = model.name.StartsWith("OperativeV03");
                foreach (var node in model.GetComponentsInChildren<Transform>())
                    if (node.name == "SOCKET_weapon_R") weaponSocket = node;
                foreach (var bone in model.GetComponentsInChildren<Transform>())
                {
                    string key = bone.name.Replace('.', '_');
                    if ((rifleOperator && (key == "spine_mid" || key.StartsWith("clavicle_"))) || key == "hips" || key == "chest" || key == "spine" || key == "head" || key.StartsWith("thigh_") || key.StartsWith("shin_") || key.StartsWith("foot_") || key.StartsWith("upper_arm_") || key.StartsWith("forearm_") || key.StartsWith("hand_"))
                    { Bones[key] = bone; rotations[bone] = bone.localRotation; positions[bone] = bone.localPosition; }
                }
                foreach (string key in new[] {"hips","chest","thigh_L","thigh_R","shin_L","shin_R","foot_L","foot_R","upper_arm_R","forearm_R","hand_R"})
                    if (!Bones.ContainsKey(key)) throw new System.InvalidOperationException("Animation rig missing " + key);
                height = Vector3.Distance(Bones["hips"].position, Bones["foot_L"].position);
                foreach (var toe in Bones["foot_L"].GetComponentsInChildren<Transform>())
                    if (toe.name == "toe_L" && Vector3.Dot((toe.position - Bones["foot_L"].position).normalized, Vector3.forward) < .5f)
                        throw new System.InvalidOperationException("Rig feet face backwards relative to actor forward");
            }
            public void Restore() { foreach (var bone in Bones.Values) { bone.localRotation = rotations[bone]; bone.localPosition = positions[bone]; } }
            void Turn(string name, Vector3 axis, float degrees) { if (Bones.TryGetValue(name, out var bone)) bone.rotation = Quaternion.AngleAxis(degrees, axis) * bone.rotation; }
            void Aim(string a, string b, Vector3 direction)
            {
                var bone = Bones[a]; var child = Bones[b];
                bone.rotation = Quaternion.FromToRotation(child.position - bone.position, direction) * bone.rotation;
            }
            public void Pose(string name, float t)
            {
                LegacyPose(name, t);
                if (rifleOperator && weaponSocket != null) RiflePose(name, t);
            }
            // Baked, in-place presentation. Movement/aim/hits remain owned by Pilot.
            void RiflePose(string name, float t)
            {
                if (name == "Death") return;
                float phase = t * Mathf.PI * 2;
                bool running = name.StartsWith("Run");
                float stride = Mathf.Sin(phase), settle = Mathf.Cos(phase * 2);
                if (running)
                {
                    Turn("hips", Vector3.up, stride * 5);
                    Turn("chest", Vector3.up, -stride * 10);
                    float forward = name == "RunForward" ? 7 : name == "RunBack" ? -4 : 3;
                    float side = name == "RunLeft" ? 4 : name == "RunRight" ? -4 : 0;
                    Turn("spine", Vector3.right, forward);
                    Turn("spine_mid", Vector3.forward, side + stride * 1.8f);
                    Turn("chest", Vector3.forward, -stride * 1.1f);
                    Bones["hips"].position += Vector3.right * (height * .012f * stride);
                    Turn("clavicle_L", Vector3.forward, stride * 2.5f);
                    Turn("clavicle_R", Vector3.forward, -stride * 2.5f);
                    Turn("head", Vector3.up, stride * 3);
                }
                else if (name == "Idle")
                {
                    Turn("hips", Vector3.forward, stride * .7f);
                    Turn("spine", Vector3.forward, -stride * .8f);
                    Turn("clavicle_L", Vector3.forward, stride * .6f);
                    Turn("clavicle_R", Vector3.forward, -stride * .6f);
                }
                // Weapon is the reference for both hands. It moves with the chest,
                // with small delayed footfall absorption instead of frozen shoulders.
                Vector3 grip = Bones["chest"].position + Vector3.right * (height * .11f)
                    + Vector3.up * (height * -.035f) + Vector3.forward * (height * .25f);
                grip += Vector3.up * (height * (running ? .008f * Mathf.Sin(phase * 2 - .55f) : .003f * stride));
                float recoil = name == "Fire" ? Mathf.Sin(t * Mathf.PI) : 0;
                grip -= Vector3.forward * (height * .035f * recoil);
                Quaternion gunRotation = Quaternion.Euler(-recoil * 3, 0, running ? stride * 1.2f : stride * .3f);
                Quaternion handRotation = gunRotation * Quaternion.Inverse(weaponSocket.localRotation);
                Vector3 wrist = grip - handRotation * Vector3.Scale(weaponSocket.localPosition, Bones["hand_R"].lossyScale);
                SolveArm("R", wrist, Vector3.right + Vector3.down * .75f);
                Bones["hand_R"].rotation = handRotation;
                Vector3 support = grip + gunRotation * new Vector3(-height * .035f, -height * .025f, height * .24f);
                Vector3 leftWrist = support - gunRotation * Vector3.forward * (height * .045f);
                float release = name == "Cast" || name == "Melee" ? Mathf.Sin(t * Mathf.PI) : 0;
                Vector3 freeWrist = Bones["hand_L"].position;
                Vector3 pole = Vector3.left + Vector3.down * .85f;
                if (release > 0)
                {
                    leftWrist = Vector3.Lerp(leftWrist, freeWrist, release);
                    pole = Vector3.Lerp(pole, (Bones["forearm_L"].position - Bones["upper_arm_L"].position).normalized, release);
                }
                SolveArm("L", leftWrist, pole);
                // Keep a neutral wrist following the support forearm; no detached hand offset.
                Turn("hand_L", Vector3.up, -5 * (1 - release));
            }
            void SolveArm(string side, Vector3 target, Vector3 pole)
            {
                var upper = Bones["upper_arm_" + side];
                var lower = Bones["forearm_" + side];
                var hand = Bones["hand_" + side];
                Vector3 shoulder = upper.position;
                float a = Vector3.Distance(shoulder, lower.position), b = Vector3.Distance(lower.position, hand.position);
                Vector3 ray = target - shoulder;
                float distance = Mathf.Clamp(ray.magnitude, Mathf.Abs(a - b) + .002f, a + b - .002f);
                Vector3 axis = ray.normalized;
                Vector3 bend = Vector3.ProjectOnPlane(pole, axis).normalized;
                float along = (a * a - b * b + distance * distance) / (2 * distance);
                Vector3 elbow = shoulder + axis * along + bend * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
                Aim("upper_arm_" + side, "forearm_" + side, elbow - shoulder);
                Aim("forearm_" + side, "hand_" + side, target - lower.position);
            }
            void LegacyPose(string name, float t)
            {
                float phase = t * Mathf.PI * 2, breath = Mathf.Sin(phase);
                Aim("upper_arm_R", "forearm_R", Vector3.forward * .65f - Vector3.up * .3f);
                Aim("forearm_R", "hand_R", Vector3.forward);
                Aim("upper_arm_L", "forearm_L", Vector3.forward * .25f - Vector3.up * .7f);
                Aim("forearm_L", "hand_L", Vector3.forward * .55f - Vector3.up * .3f);
                if (name.StartsWith("Run"))
                {
                    Vector3 direction = name == "RunBack" ? Vector3.back : name == "RunLeft" ? Vector3.left : name == "RunRight" ? Vector3.right : Vector3.forward;
                    Vector3 axis = Vector3.Cross(Vector3.up, direction);
                    float stride = Mathf.Sin(phase);
                    Turn("thigh_L", axis, -stride * 40); Turn("thigh_R", axis, stride * 40);
                    float leftBend = Mathf.Max(0, stride) * 65, rightBend = Mathf.Max(0, -stride) * 65;
                    Turn("shin_L", Vector3.right, leftBend);
                    Turn("shin_R", Vector3.right, rightBend);
                    Turn("foot_L", axis, stride * 40); Turn("foot_R", axis, -stride * 40);
                    Turn("foot_L", Vector3.right, -leftBend); Turn("foot_R", Vector3.right, -rightBend);
                    Bones["hips"].position += Vector3.down * (height * (.055f + .025f * Mathf.Cos(phase * 2)));
                    Turn("chest", Vector3.up, stride * 7);
                }
                else if (name == "Idle") { Bones["chest"].position += Vector3.up * (height * .008f * breath); Turn("head", Vector3.up, breath * 2); }
                else if (name == "Fire") { float recoil = Mathf.Sin(t * Mathf.PI); Turn("upper_arm_R", Vector3.right, -recoil * 14); Turn("chest", Vector3.right, -recoil * 5); }
                else if (name == "Melee")
                {
                    float swing = Mathf.Sin(t * Mathf.PI);
                    Turn("chest", Vector3.up, -35 + 85 * swing);
                    Aim("upper_arm_L", "forearm_L", Vector3.forward * swing + Vector3.left * (1 - swing));
                    Aim("forearm_L", "hand_L", Vector3.forward + Vector3.right * (swing - .5f));
                    Turn("thigh_L", Vector3.right, -swing * 20); Turn("shin_L", Vector3.right, swing * 25);
                }
                else if (name == "Cast") { float throwPose = Mathf.Sin(t * Mathf.PI); Turn("upper_arm_L", Vector3.right, -throwPose * 125); Turn("forearm_L", Vector3.right, -throwPose * 55); Turn("chest", Vector3.up, throwPose * 25); }
                else if (name == "Dash") { Turn("hips", Vector3.right, 35); Turn("thigh_L", Vector3.right, -60); Turn("shin_L", Vector3.right, 85); Turn("thigh_R", Vector3.right, 40); Bones["hips"].position += Vector3.down * height * .18f; }
                else if (name == "Hit") { Turn("chest", Vector3.right, -Mathf.Sin(t * Mathf.PI) * 22); }
                else if (name == "Death")
                {
                    float fall = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .8f));
                    Turn("hips", Vector3.right, -88 * fall);
                    Bones["hips"].position += (Vector3.down * .83f + Vector3.back * .25f) * height * fall;
                    Turn("shin_L", Vector3.right, 30 * Mathf.Sin(t * Mathf.PI));
                    Turn("upper_arm_R", Vector3.forward, 45 * fall); Turn("upper_arm_L", Vector3.forward, -45 * fall);
                }
            }
        }
    }
}
