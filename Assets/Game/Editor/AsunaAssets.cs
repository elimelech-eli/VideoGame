using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace VideoGame.Editor
{
    public static class AsunaAssets
    {
        const string Source = "Assets/FreeTestCharacterAsuna/";
        const string Prefab = "Assets/Game/Resources/Detailed/Prefabs/OperativeAsuna.prefab";
        const string Animations = "Assets/Game/Resources/AsunaAnimations/";
        const string Weapons = "Assets/Game/Resources/AsunaWeaponAnimations/";
        public static void BuildAndRender(){Build();Render();}
        static readonly Dictionary<string,string> Names = new Dictionary<string,string>
        {
            {"Spine1","hips"},{"Spine2","spine"},{"Spine3","chest"},{"Head","head"},{"Neck","neck"},
            {"Hip_L","thigh_L"},{"Hip_R","thigh_R"},{"Shin_L","shin_L"},{"Shin_R","shin_R"},
            {"Foot_L","foot_L"},{"Foot_R","foot_R"},{"Toe1_L","toe_L"},{"Toe1_R","toe_R"},
            {"Bicep_L","upper_arm_L"},{"Bicep_R","upper_arm_R"},{"4arm_L","forearm_L"},{"4arm_R","forearm_R"},
            {"Hand_L","hand_L"},{"Hand_R","hand_R"},{"cShrugger_L","clavicle_L"},{"cShrugger_R","clavicle_R"}
        };
        [MenuItem("Nexus/Build Asuna trial character")]
        public static void Build()
        {
            Directory.CreateDirectory(Animations); Directory.CreateDirectory(Weapons); AssetDatabase.Refresh();
            ConvertMaterials();
            BuildWeapons();
            var source = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source+"Prefabs/FreeTestCharacterAsunaMasterPrefab.prefab"));
            var target = Object.Instantiate(source); target.name="OperativeAsuna";
            var sourceBones=source.GetComponentsInChildren<Transform>(true).ToDictionary(t=>AnimationUtility.CalculateTransformPath(t,source.transform));
            var targetBones=target.GetComponentsInChildren<Transform>(true).ToDictionary(t=>AnimationUtility.CalculateTransformPath(t,target.transform));
            var deform=new HashSet<Transform>();
            foreach(var skin in source.GetComponentsInChildren<SkinnedMeshRenderer>(true))foreach(var bone in skin.bones)
                for(var t=bone;t!=null&&t!=source.transform;t=t.parent)deform.Add(t);
            var pairs=sourceBones.Where(p=>deform.Contains(p.Value)).Select(p=>(source:p.Value,target:targetBones[p.Key])).ToArray();
            foreach(var a in target.GetComponentsInChildren<Animator>())Object.DestroyImmediate(a);
            foreach(var t in targetBones.Values)
            {
                if(Names.TryGetValue(t.name,out string name))t.name=name;
                foreach(string side in new[]{"L","R"})for(int n=1;n<=3;n++)
                    foreach(var finger in new[]{("Thumb","thumb"),("Pointer","index"),("Middle","middle"),("Ring","ring"),("Pinky","pinky")})
                        if(t.name==finger.Item1+n+"_"+side)t.name="hand"+finger.Item2+n+"_"+side;
            }
            Transform Find(string name)=>target.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
            var mid=new GameObject("spine_mid").transform;mid.SetParent(Find("spine"),false);mid.position=Find("chest").position;
            Find("chest").SetParent(mid,true);
            foreach(var t in target.GetComponentsInChildren<Transform>(true))
            {
                if(t.name.StartsWith("ScifiRifle")||t.name.StartsWith("ScifiPistol")||t.name=="AsunaHair"||t.name=="AsunaHairTie"||t.name.StartsWith("Eyeball"))t.gameObject.SetActive(false);
                if(t.name.StartsWith("AsunaArmor"))t.gameObject.SetActive(true);
            }
            var rig=target.AddComponent<PlayerRig>();rig.SwordRight=Find("Melee_Socket_R").localRotation;rig.SwordLeft=Find("Melee_Socket_L").localRotation;
            foreach(var r in target.GetComponentsInChildren<SkinnedMeshRenderer>())r.updateWhenOffscreen=true;
            var socket=new GameObject("SOCKET_weapon_R").transform;socket.SetParent(Find("hand_R"),false);
            socket.localRotation=Find("Firearm_SocketRifle_R").localRotation;
            FitHelmet(Find("head"));
            var maps=new[]{("Idle","001Idle","Idle1"),("RunForward","003Run","Run"),("RunBack","003Run","RunBack"),
                ("RunLeft","003Run","RunStrafeLeft"),("RunRight","003Run","RunStrafeRight"),("Fire","011Rifle","RifleFire"),
                ("Melee","023OneHandMeleeWeapon","AttackRightHand"),("Cast","007Misc","PickUpItemTable"),
                ("Dash","004Jump","JumpStart"),("Hit","008Damage","HitFront"),("Death","006Death","Death1")};
            foreach(var map in maps)Bake(map.Item1,map.Item2,map.Item3,Animations,source,target,pairs);
            // Preserve the sampled native idle as the neutral base for weapon layers.
            Resources.Load<AnimationClip>("AsunaAnimations/Idle").SampleAnimation(target,0);
            PrefabUtility.SaveAsPrefabAsset(target,Prefab);
            CombatAnimationBuilder.BuildFor(Prefab,"AsunaAnimations",Weapons);
            Bake("SwordR","023OneHandMeleeWeapon","AttackRightHand",Weapons,source,target,pairs,PlayerWeaponAnimation.SwingDuration);
            Bake("SwordL","023OneHandMeleeWeapon","AttackLeftHand",Weapons,source,target,pairs,PlayerWeaponAnimation.SwingDuration);
            const string grips="Assets/Game/Resources/AsunaGrips/";Directory.CreateDirectory(grips);AssetDatabase.Refresh();
            foreach(var pose in new[]{("HandgunR","010Pistol","PistolAim",0),("HandgunL","023OneHandMeleeWeapon","CombatStance1HBothHands",1),
                ("RifleR","011Rifle","RifleAim",0),("RifleL","011Rifle","RifleAim",1),
                ("SwordR","023OneHandMeleeWeapon","CombatStance1H",0),("SwordL","023OneHandMeleeWeapon","CombatStance1HBothHands",1)})
                Bake(pose.Item1,pose.Item2,pose.Item3,grips,source,target,pairs,.01f,true,pose.Item4);
            Object.DestroyImmediate(source);Object.DestroyImmediate(target);AssetDatabase.SaveAssets();
            Debug.Log("ASUNA_READY: native locomotion, damage, death and independent melee clips; canonical equipment rig.");
        }
        static void Bake(string name,string file,string take,string folder,GameObject source,GameObject target,(Transform source,Transform target)[] pairs,float duration=0,bool fingersOnly=false,int hand=0)
        {
            var clip=AssetDatabase.LoadAllAssetsAtPath(Source+"FBX/Animations/"+file+".fbx").OfType<AnimationClip>().First(c=>c.name==take);
            var animator=source.GetComponentInChildren<Animator>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var graph=PlayableGraph.Create("Asuna bake "+name);graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            try
            {
                var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(false);
                AnimationPlayableOutput.Create(graph,"pose",animator).SetSourcePlayable(playable);graph.Play();
                float length=duration>0?duration:clip.length;int frames=Mathf.CeilToInt(length*60);
                var curves=pairs.Select(p=>Enumerable.Range(0,7).Select(_=>new AnimationCurve()).ToArray()).ToArray();
                for(int frame=0;frame<=frames;frame++)
                {
                    float t=(float)frame/frames;float native=t;
                    if(name=="SwordR"||name=="SwordL")
                    {
                        float time=t*length;
                        // The source cut's strongest turn/weight transfer is
                        // halfway through its take. Align it to the game's hit.
                        native=time<=.12f?Mathf.Lerp(0,.25f,time/.12f):time<=PlayerWeaponAnimation.SwordContactTime?
                            Mathf.Lerp(.25f,.5f,Mathf.InverseLerp(.12f,PlayerWeaponAnimation.SwordContactTime,time)):
                            Mathf.Lerp(.5f,1,Mathf.InverseLerp(PlayerWeaponAnimation.SwordContactTime,length,time));
                    }
                    playable.SetTime(fingersOnly?0:native*clip.length);graph.Evaluate(0);
                    for(int b=0;b<pairs.Length;b++)
                    {
                        var p=pairs[b];p.target.SetPositionAndRotation(p.source.position,p.source.rotation);
                        var q=p.target.localRotation;var v=p.target.localPosition;float[] values={q.x,q.y,q.z,q.w,v.x,v.y,v.z};
                        for(int k=0;k<7;k++)curves[b][k].AddKey(t*length,values[k]);
                    }
                }
                var baked=new AnimationClip{name=name,legacy=true,frameRate=60,wrapMode=name=="Idle"||name.StartsWith("Run")?WrapMode.Loop:WrapMode.ClampForever};
                string[] properties={"localRotation.x","localRotation.y","localRotation.z","localRotation.w","localPosition.x","localPosition.y","localPosition.z"};
                for(int b=0;b<pairs.Length;b++)for(int k=0;k<7;k++)
                {
                    string bone=pairs[b].target.name;
                    if(fingersOnly&&(!bone.StartsWith("hand")||bone.StartsWith("hand_")||!bone.EndsWith(hand==0?"_R":"_L")||k>=4))continue;
                    var curve=Reduce(curves[b][k]);
                    baked.SetCurve(AnimationUtility.CalculateTransformPath(pairs[b].target,target.transform),typeof(Transform),properties[k],curve);
                }
                baked.EnsureQuaternionContinuity();string path=folder+name+".anim";
                var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if(old==null)AssetDatabase.CreateAsset(baked,path);else{EditorUtility.CopySerialized(baked,old);Object.DestroyImmediate(baked);}
            }
            finally{graph.Destroy();}
        }
        static AnimationCurve Reduce(AnimationCurve curve)
        {
            var keys=curve.keys;var keep=new SortedSet<int>{0,keys.Length-1};
            void Split(int a,int b)
            {
                float error=.0001f;int worst=-1;
                for(int i=a+1;i<b;i++)
                {
                    float expected=Mathf.Lerp(keys[a].value,keys[b].value,Mathf.InverseLerp(keys[a].time,keys[b].time,keys[i].time));
                    float distance=Mathf.Abs(expected-keys[i].value);if(distance>error){error=distance;worst=i;}
                }
                if(worst<0)return;keep.Add(worst);Split(a,worst);Split(worst,b);
            }
            Split(0,keys.Length-1);var result=new AnimationCurve(keep.Select(i=>keys[i]).ToArray());
            for(int i=0;i<result.length;i++){AnimationUtility.SetKeyLeftTangentMode(result,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(result,i,AnimationUtility.TangentMode.Linear);}
            return result;
        }
        static void BuildWeapons()
        {
            const string folder="Assets/Game/Resources/AsunaEquipment/";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            foreach(var entry in new[]{("Rifle","ScifiRifleWLT78"),("Handgun","ScifiPistolMNL21")})
            {
                var gun=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source+"Prefabs/CharacterStandardEquipment/"+entry.Item2+"MasterPrefab.prefab"));gun.name=entry.Item1;
                foreach(var animator in gun.GetComponentsInChildren<Animator>())Object.DestroyImmediate(animator);
                var marker=new GameObject("Muzzle").transform;marker.SetParent(gun.transform,false);
                var bounds=Art.RenderedBounds(gun);marker.localPosition=new Vector3(0,entry.Item1=="Rifle"?.075f:.065f,bounds.max.z);
                PrefabUtility.SaveAsPrefabAsset(gun,folder+entry.Item1+".prefab");Object.DestroyImmediate(gun);
            }
        }
        static void FitHelmet(Transform head)
        {
            var original=Object.Instantiate(Resources.Load<GameObject>("Detailed/Prefabs/OperativeV03"));
            var skin=original.GetComponentsInChildren<SkinnedMeshRenderer>().First();var baked=new Mesh();skin.BakeMesh(baked);
            int bone=System.Array.FindIndex(skin.bones,t=>t.name=="head");var weights=skin.sharedMesh.boneWeights;
            bool IsHead(int v){var w=weights[v];return (w.boneIndex0==bone?w.weight0:0)+(w.boneIndex1==bone?w.weight1:0)+(w.boneIndex2==bone?w.weight2:0)+(w.boneIndex3==bone?w.weight3:0)>.5f;}
            var triangles=new List<int[]>();var used=new SortedSet<int>();
            for(int sub=0;sub<baked.subMeshCount;sub++)
            {
                var list=new List<int>();var all=baked.GetTriangles(sub);
                for(int i=0;i<all.Length;i+=3)if(IsHead(all[i])&&IsHead(all[i+1])&&IsHead(all[i+2]))for(int k=0;k<3;k++){list.Add(all[i+k]);used.Add(all[i+k]);}
                triangles.Add(list.ToArray());
            }
            var indices=used.ToArray();var remap=indices.Select((v,i)=>(v,i)).ToDictionary(p=>p.v,p=>p.i);
            var mesh=new Mesh{name="Fitted operative helmet"};var vertices=baked.vertices;var normals=baked.normals;
            mesh.vertices=indices.Select(i=>skin.transform.TransformPoint(vertices[i])).ToArray();mesh.normals=indices.Select(i=>skin.transform.TransformDirection(normals[i])).ToArray();
            mesh.subMeshCount=triangles.Count;for(int i=0;i<triangles.Count;i++)mesh.SetTriangles(triangles[i].Select(v=>remap[v]).ToArray(),i);mesh.RecalculateBounds();
            string path="Assets/Game/Resources/AsunaEquipment/Helmet.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}
            var helmet=new GameObject("Fitted closed helmet");helmet.AddComponent<MeshFilter>().sharedMesh=mesh;helmet.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
            helmet.transform.SetParent(head,false);helmet.transform.rotation=Quaternion.identity;
            float scale=.34f/mesh.bounds.size.y;helmet.transform.localScale=Vector3.one*scale;
            helmet.transform.position=head.position+Vector3.up*.08f-mesh.bounds.center*scale;
            Object.DestroyImmediate(baked);Object.DestroyImmediate(original);
        }
        static void ConvertMaterials()
        {
            foreach(string path in AssetDatabase.FindAssets("t:Material",new[]{Source}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var mat=AssetDatabase.LoadAssetAtPath<Material>(path);mat.shader=Shader.Find("Standard");
                mat.EnableKeyword("_NORMALMAP");mat.EnableKeyword("_METALLICGLOSSMAP");
                mat.SetFloat("_Glossiness",.4f);mat.SetFloat("_GlossMapScale",.55f);
                if(mat.name.Contains("Hair")||mat.name.Contains("Lashes"))
                {mat.SetFloat("_Mode",1);mat.SetInt("_SrcBlend",1);mat.SetInt("_DstBlend",0);mat.SetInt("_ZWrite",1);mat.EnableKeyword("_ALPHATEST_ON");mat.renderQueue=2450;}
                EditorUtility.SetDirty(mat);
            }
        }
        public static void Render()
        {
            var probe=Object.Instantiate(Resources.Load<GameObject>(PlayerRig.PrefabPath));
            var sword=Resources.Load<AnimationClip>("AsunaWeaponAnimations/SwordR");
            var chest=probe.GetComponentsInChildren<Transform>().First(t=>t.name=="chest");
            var hips=probe.GetComponentsInChildren<Transform>().First(t=>t.name=="hips");
            sword.SampleAnimation(probe,0);var initial=chest.rotation;var origin=hips.position;var metrics=new System.Text.StringBuilder();
            for(int i=0;i<=12;i++){float time=sword.length*i/12;sword.SampleAnimation(probe,time);metrics.AppendLine(time+","+Quaternion.Angle(initial,chest.rotation)+","+Vector3.Distance(origin,hips.position));}
            Directory.CreateDirectory("artifacts/asuna");File.WriteAllText("artifacts/asuna/sword-timing.csv",metrics.ToString());Object.DestroyImmediate(probe);
            RenderLoadout("rifle",null,"character-review");
            RenderLoadout("sword","shield","sword-shield");
            RenderLoadout("pistol","pistol","dual-pistols");
        }
        static void RenderLoadout(string main,string off,string filename)
        {
            var model=Object.Instantiate(Resources.Load<GameObject>(PlayerRig.PrefabPath));
            Resources.Load<AnimationClip>("AsunaAnimations/Idle").SampleAnimation(model,0);
            var profile=VideoGame.Core.GearProfile.Starter();
            profile.Equipped[3]=VideoGame.Core.GearCatalog.Roll(new System.Random(1),1,VideoGame.Core.GearQuality.White,main);
            profile.Equipped[4]=off==null?null:VideoGame.Core.GearCatalog.Roll(new System.Random(2),1,VideoGame.Core.GearQuality.White,off);
            new PreviewLoadout(model.transform).Apply(profile);
            var bounds=Art.RenderedBounds(model);
            var cameraObject=new GameObject("Asuna review camera");var camera=cameraObject.AddComponent<Camera>();
            camera.transform.position=bounds.center+new Vector3(1.8f,.3f,3.4f);camera.transform.LookAt(bounds.center);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.05f,.07f);camera.fieldOfView=34;
            var key=new GameObject("Review key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.6f;key.transform.rotation=Quaternion.Euler(30,200,0);
            var fill=new GameObject("Review fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.7f;fill.color=new Color(.4f,.7f,1);fill.transform.rotation=Quaternion.Euler(10,70,0);
            var rt=new RenderTexture(800,1000,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(800,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,800,1000),0,0);image.Apply();
            Directory.CreateDirectory("artifacts/asuna");File.WriteAllBytes("artifacts/asuna/"+filename+".png",image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
            Object.DestroyImmediate(model);Object.DestroyImmediate(cameraObject);Object.DestroyImmediate(key.gameObject);Object.DestroyImmediate(fill.gameObject);
        }
        public static void Inspect()
        {
            var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source + "Prefabs/FreeTestCharacterAsunaMasterPrefab.prefab"));
            var report = new System.Text.StringBuilder();
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
                report.AppendLine(AnimationUtility.CalculateTransformPath(t, model.transform) + " | " + t.localPosition + " | active=" + t.gameObject.activeSelf);
            foreach (var a in model.GetComponentsInChildren<Animator>()) report.AppendLine("AVATAR " + a.name + " " + a.isHuman + " " + a.avatar?.isValid);
            foreach (var r in model.GetComponentsInChildren<Renderer>(true)) report.AppendLine("RENDER " + r.name + " " + string.Join(",", r.sharedMaterials.Select(m => m == null ? "NULL" : m.name)));
            foreach (string path in AssetDatabase.FindAssets("t:AnimationClip", new[]{Source + "FBX/Animations"}).Select(AssetDatabase.GUIDToAssetPath).Distinct())
                foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()) report.AppendLine("CLIP " + clip.name + " " + clip.length + " " + path);
            Directory.CreateDirectory("artifacts/asuna"); File.WriteAllText("artifacts/asuna/inspection.txt", report.ToString());
            Object.DestroyImmediate(model);
        }
    }
}
