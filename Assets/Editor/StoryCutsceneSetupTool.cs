#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.SceneManagement;

public class StoryCutsceneSetupTool : EditorWindow
{
    [MenuItem("ThanhGiong/1. Tự Động Tạo Cảnh Mở Đầu (Gióng Nhỏ)")]
    public static void GenerateOpeningCutscene()
    {
        CreateCutscene("Opening_Cutscene_Manager", new DialogueLine[] {
            new DialogueLine { characterName = "Thánh Gióng", text = "Nước nhà lâm nguy, làm trai phải ra sức đền nợ nước! Ta đã sẵn sàng!" },
            new DialogueLine { characterName = "Mẹ Thánh Gióng", text = "Con trai, hãy bảo trọng. Dân làng luôn hướng về con." }
        }, "Assets/Models/Player/thanhgiongembe.prefab", "Assets/Models/Player/thanhgiongembe.fbx", "ThanhGiong_Nho", "Run", "GrowUp");
    }

    [MenuItem("ThanhGiong/2. Tự Động Tạo Cảnh Sứ Giả (Map 1 -> 2)")]
    public static void GenerateMessengerCutscene()
    {
        CreateCutscene("Messenger_Cutscene_Manager", new DialogueLine[] {
            new DialogueLine { characterName = "Sứ Giả", text = "Báo!... Tin mừng! Tráng sĩ ơi, giặc dữ ở khu vực làng ta đã bị ngài đuổi sạch rồi! Dân làng đã được bình yên!" },
            new DialogueLine { characterName = "Thánh Gióng", text = "Sứ giả chớ mừng vội. Căn nguyên bờ cõi chưa yên, toán quân chính của chúng đang ở đâu?" },
            new DialogueLine { characterName = "Sứ Giả", text = "Đây là bản đồ quân cơ của triều đình. Phía trước là vùng đồng bằng, quân địch tụ tập rất đông!" }
        }, "Assets/Models/Player/sugia.fbx", "Assets/Models/Player/sugia.prefab", "SuGia", "Run", "Talk");
    }

    private static void CreateCutscene(string managerName, DialogueLine[] lines, string modelPath1, string modelPath2, string modelName, string anim1, string anim2)
    {
        // 1. Tạo thư mục chứa Dialogue nếu chưa có
        string resPath = Application.dataPath + "/Resources";
        string dialogueFolder = resPath + "/Dialogues";
        
        if (!Directory.Exists(resPath)) Directory.CreateDirectory(resPath);
        if (!Directory.Exists(dialogueFolder)) Directory.CreateDirectory(dialogueFolder);
        
        AssetDatabase.Refresh();

        // 2. Tự động gắn Voice Clip mp3 vào từng dòng thoại (nếu file tồn tại)
        string[] voiceFiles = new string[] { "Giong_Line1", "Mom_Line2", "SuGia_Line1", "Giong_Line2", "SuGia_Line3" };
        for (int i = 0; i < lines.Length; i++)
        {
            if (i < voiceFiles.Length)
            {
                string clipPath = "Assets/Resources/Dialogues/" + voiceFiles[i] + ".mp3";
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                if (clip != null)
                {
                    lines[i].voiceClip = clip;
                    Debug.Log("🔊 Đã gắn voice clip: " + voiceFiles[i] + " vào dòng " + (i + 1));
                }
            }
        }

        // 3. Tạo Asset Dialogue
        DialogueData dialogueData = ScriptableObject.CreateInstance<DialogueData>();
        dialogueData.lines = lines;
        
        string assetPath = "Assets/Resources/Dialogues/" + managerName + "_Dialogue.asset";
        AssetDatabase.CreateAsset(dialogueData, assetPath);
        AssetDatabase.SaveAssets();

        // 3. Tạo GameObject Quản lý Cutscene
        GameObject cutsceneGO = new GameObject(managerName);
        StoryCutsceneManager manager = cutsceneGO.AddComponent<StoryCutsceneManager>();
        manager.playOnStart = false; // Tùy chỉnh sau

        // ★ Tạo AudioSource trực tiếp trên Manager để phát lồng tiếng
        AudioSource voiceSource = cutsceneGO.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;
        voiceSource.volume = 1f;
        voiceSource.ignoreListenerPause = true;

        // 4. Tìm và Instantiate Model
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath1);
        if (prefab == null) prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath2);

        Animator modelAnimator = null;
        if (prefab != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = modelName;
            instance.transform.position = Vector3.zero;
            modelAnimator = instance.GetComponent<Animator>();
            if (modelAnimator == null) modelAnimator = instance.AddComponent<Animator>();
        }
        else
        {
            Debug.LogWarning("AI không tìm thấy model tại " + modelPath1 + ". Tạo khối Cube tạm thời!");
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = modelName + "_Placeholder";
            modelAnimator = cube.AddComponent<Animator>();
        }

        // 5. Cấu hình Steps - GẮN TRỰC TIẾP voice clip + AudioSource vào từng step
        manager.cutsceneSteps = new CutsceneStep[2];

        // Step 1: Chạy ra & Mở thoại + PHÁT VOICE CLIP DÒNG 1
        manager.cutsceneSteps[0] = new CutsceneStep();
        manager.cutsceneSteps[0].stepName = "Xuất hiện và Hội thoại";
        manager.cutsceneSteps[0].targetAnimator = modelAnimator;
        manager.cutsceneSteps[0].animationTriggerName = anim1;
        manager.cutsceneSteps[0].dialogueData = dialogueData;
        manager.cutsceneSteps[0].waitForDialogueFinish = true;
        manager.cutsceneSteps[0].voiceAudioSource = voiceSource;
        // Gắn voice clip đầu tiên (nếu có)
        if (lines.Length > 0 && lines[0].voiceClip != null)
            manager.cutsceneSteps[0].voiceClip = lines[0].voiceClip;

        // Step 2: Đổi anim sau thoại
        manager.cutsceneSteps[1] = new CutsceneStep();
        manager.cutsceneSteps[1].stepName = "Hoạt ảnh tiếp theo";
        manager.cutsceneSteps[1].targetAnimator = modelAnimator;
        manager.cutsceneSteps[1].animationTriggerName = anim2;
        manager.cutsceneSteps[1].waitAfterStep = 1.5f;

        // 6. Highlight object mới tạo để user thấy ngay
        Selection.activeGameObject = cutsceneGO;
        EditorGUIUtility.PingObject(cutsceneGO);

        Debug.Log("✅ [Auto Setup] Đã tạo thành công " + managerName + " kèm Voice Clip!");
    }
}
#endif
