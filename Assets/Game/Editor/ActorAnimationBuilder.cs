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
        static void Build(bool force)
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Resources/Detailed/Prefabs/Operative.prefab");
            if (source == null) return;
            foreach (string name in Names)
            {
                string path = Folder + name + ".anim";
                if (!force && File.Exists(path)) continue;
                var actor = new GameObject("Animation authoring rig");
                var visual = new GameObject("Visual"); visual.transform.SetParent(actor.transform); visual.transform.localRotation = Quaternion.Euler(0, 180, 0);
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
                    for (int i = 0; i < 7; i++) clip.SetCurve(AnimationUtility.CalculateTransformPath(pair.Key, model.transform), typeof(Transform), properties[i], pair.Value[i]);
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
            public Rig(Transform model)
            {
                foreach (var bone in model.GetComponentsInChildren<Transform>())
                {
                    string key = bone.name.Replace('.', '_');
                    if (key == "hips" || key == "chest" || key == "spine" || key == "head" || key.StartsWith("thigh_") || key.StartsWith("shin_") || key.StartsWith("foot_") || key.StartsWith("upper_arm_") || key.StartsWith("forearm_") || key.StartsWith("hand_"))
                    { Bones[key] = bone; rotations[bone] = bone.localRotation; positions[bone] = bone.localPosition; }
                }
                foreach (string key in new[] {"hips","chest","thigh_L","thigh_R","shin_L","shin_R","foot_L","foot_R","upper_arm_R","forearm_R","hand_R"})
                    if (!Bones.ContainsKey(key)) throw new System.InvalidOperationException("Animation rig missing " + key);
                height = Vector3.Distance(Bones["hips"].position, Bones["foot_L"].position);
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
                    Turn("thigh_L", axis, stride * 40); Turn("thigh_R", axis, -stride * 40);
                    Turn("shin_L", Vector3.right, -Mathf.Max(0, -stride) * 65);
                    Turn("shin_R", Vector3.right, -Mathf.Max(0, stride) * 65);
                    Turn("foot_L", axis, -stride * 20); Turn("foot_R", axis, stride * 20);
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
                    Turn("thigh_L", Vector3.right, -swing * 20); Turn("shin_L", Vector3.right, -swing * 25);
                }
                else if (name == "Cast") { float throwPose = Mathf.Sin(t * Mathf.PI); Turn("upper_arm_L", Vector3.right, -throwPose * 125); Turn("forearm_L", Vector3.right, -throwPose * 55); Turn("chest", Vector3.up, throwPose * 25); }
                else if (name == "Dash") { Turn("hips", Vector3.right, 35); Turn("thigh_L", Vector3.right, -60); Turn("shin_L", Vector3.right, -85); Turn("thigh_R", Vector3.right, 40); Bones["hips"].position += Vector3.down * height * .18f; }
                else if (name == "Hit") { Turn("chest", Vector3.right, -Mathf.Sin(t * Mathf.PI) * 22); }
                else if (name == "Death")
                {
                    float fall = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .8f));
                    Turn("hips", Vector3.right, -88 * fall);
                    Bones["hips"].position += (Vector3.down * .83f + Vector3.back * .25f) * height * fall;
                    Turn("shin_L", Vector3.right, -30 * Mathf.Sin(t * Mathf.PI));
                    Turn("upper_arm_R", Vector3.forward, 45 * fall); Turn("upper_arm_L", Vector3.forward, -45 * fall);
                }
            }
        }
    }
}
