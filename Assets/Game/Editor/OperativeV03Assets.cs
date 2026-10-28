using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VideoGame.Editor
{
    public static class OperativeV03Assets
    {
        const string Root = "Assets/Game/Resources/Detailed/OperativeV03/";
        public const string Prefab = "Assets/Game/Resources/Detailed/Prefabs/OperativeV03.prefab";
        const string Clips = "Assets/Game/Resources/OperatorAnimations/";
        [Serializable] sealed class Roles { public Role[] materials; }
        [Serializable] sealed class Role
        {
            public string name;
            public float[] color, emission;
            public float metallic, roughness, emissionStrength;
        }
        static void AttachWeapon(GameObject model)
        {
            Transform hand = null, forearm = null;
            foreach (var bone in model.GetComponentsInChildren<Transform>())
            {
                if (bone.name == "hand_R") hand = bone;
                if (bone.name == "forearm_R") forearm = bone;
            }
            if (hand == null || forearm == null) throw new InvalidOperationException("Operator weapon attachment requires right hand and forearm");
            var socket = new GameObject("SOCKET_weapon_R").transform;
            socket.SetParent(hand, false);
            Vector3 forward = (hand.position - forearm.position).normalized;
            socket.position = hand.position + forward * .045f;
            socket.rotation = Quaternion.LookRotation(forward, model.transform.forward);
            Transform hips = null, foot = null;
            foreach (var bone in model.GetComponentsInChildren<Transform>())
            {
                if (bone.name == "hips") hips = bone;
                if (bone.name == "foot_L") foot = bone;
            }
            float torsoScale = Vector3.Distance(hips.position, foot.position);
            var support = new GameObject("SOCKET_support_L").transform;
            support.SetParent(socket, false);
            support.position = socket.position + socket.rotation * (new Vector3(-.035f, -.025f, .24f) * torsoScale);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Resources/Art/Blaster/blaster-e.fbx");
            if (source == null) throw new InvalidOperationException("Missing existing licensed weapon mesh");
            var weapon = UnityEngine.Object.Instantiate(source, socket); weapon.name = "Equipped rifle";
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
            var bounds = new Bounds(); bool first = true;
            foreach (var renderer in weapon.GetComponentsInChildren<Renderer>())
            {
                if (first) { bounds = renderer.bounds; first = false; } else bounds.Encapsulate(renderer.bounds);
            }
            weapon.transform.localScale *= .60f / Mathf.Max(.001f, bounds.size.magnitude);
            string materialPath = Root + "Materials/EquippedRifle.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) { material = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material, materialPath); }
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/Resources/Art/Blaster/Textures/colormap.png");
            material.color = new Color(.55f,.60f,.66f); material.SetFloat("_Metallic",.45f); material.SetFloat("_Glossiness",.35f);
            EditorUtility.SetDirty(material);
            foreach (var renderer in weapon.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = material;
        }
        public static void Build()
        {
            if (File.Exists(Prefab) && File.Exists(Clips + "Death.anim")) return;
            Rebuild();
        }
        [MenuItem("Nexus/Rebuild approved operator v03")]
        public static void Rebuild()
        {
            AssetDatabase.Refresh();
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "OperativeV03.fbx");
            if (source == null) throw new InvalidOperationException("Missing approved operator FBX");
            var roles = JsonUtility.FromJson<Roles>(File.ReadAllText(Root + "materials.json"));
            Directory.CreateDirectory(Root + "Materials");
            var model = UnityEngine.Object.Instantiate(source); model.name = "OperativeV03";
            foreach (var animator in model.GetComponentsInChildren<Animator>()) UnityEngine.Object.DestroyImmediate(animator);
            foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                // The export manifest records this mesh's material-slot order.
                var materials = new Material[skin.sharedMesh.subMeshCount];
                if (materials.Length != roles.materials.Length) throw new InvalidOperationException("Operator material slot count changed; re-export its manifest");
                for (int i = 0; i < materials.Length; i++)
                {
                    Role role = roles.materials[i];
                    string path = Root + "Materials/" + role.name.Replace(' ', '_') + ".mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null) { material = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material, path); }
                    material.color = new Color(role.color[0], role.color[1], role.color[2], 1).gamma;
                    material.SetFloat("_Metallic", role.metallic); material.SetFloat("_Glossiness", 1 - role.roughness);
                    if (role.emissionStrength > 0)
                    {
                        material.EnableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor", new Color(role.emission[0], role.emission[1], role.emission[2]).gamma * role.emissionStrength);
                    }
                    material.enableInstancing = true; EditorUtility.SetDirty(material); materials[i] = material;
                }
                skin.sharedMaterials = materials;
                skin.updateWhenOffscreen = true;
            }
            AttachWeapon(model);
            PrefabUtility.SaveAsPrefabAsset(model, Prefab); UnityEngine.Object.DestroyImmediate(model);
            ActorAnimationBuilder.BuildFor(Prefab, Clips, true);
            AssetDatabase.SaveAssets();
            Debug.Log("OPERATIVE_V03_READY: approved mesh, materials and eleven rig-specific action clips.");
        }
    }
}
