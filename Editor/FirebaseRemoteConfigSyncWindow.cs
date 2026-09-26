using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using UnityEditor;
using UnityEngine;

namespace Unity.Firebase.Editor
{
    /// <summary>
    /// Công cụ Unity Editor cho phép kéo toàn bộ Template Remote Config trực tiếp từ Google Firebase Cloud
    /// thông qua OAuth2 Service Account Key và tự động xuất ra file Resources/RemoteConfig.json.
    /// </summary>
    public class FirebaseRemoteConfigSyncWindow : EditorWindow
    {
        private string _serviceAccountKeyPath = "";
        private string _projectId = "";
        private string _targetResourcePath = "Assets/Resources/RemoteConfig.json";
        private Vector2 _scrollPos;
        private string _statusMessage = "";
        private MessageType _statusType = MessageType.None;
        private string _previewJson = "";
        private bool _isSyncing = false;

        private const string PREF_KEY_PATH = "WASD_FIREBASE_KEY_PATH";
        private const string PREF_PROJECT_ID = "WASD_FIREBASE_PROJECT_ID";
        private const string PREF_EXPORT_PATH = "WASD_FIREBASE_EXPORT_PATH";

        [MenuItem("Unity Core/Firebase/Remote Config Sync Tool", false, 15)]
        public static void ShowWindow()
        {
            var window = GetWindow<FirebaseRemoteConfigSyncWindow>("Firebase Remote Config");
            window.minSize = new Vector2(520, 520);
            window.Show();
        }

        private void OnEnable()
        {
            _serviceAccountKeyPath = EditorPrefs.GetString(PREF_KEY_PATH, "");
            _projectId = EditorPrefs.GetString(PREF_PROJECT_ID, "");
            _targetResourcePath = EditorPrefs.GetString(PREF_EXPORT_PATH, "Assets/Resources/RemoteConfig.json");

            if (string.IsNullOrEmpty(_projectId) && File.Exists(_serviceAccountKeyPath))
            {
                ExtractProjectId();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            GUILayout.Label("Unity Firebase - Remote Config Sync", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Đồng bộ hóa các thông số cân bằng game từ Firebase Remote Config Console về file Local Resources.\n" +
                "Yêu cầu: File Service Account Key (.json) được tải từ Firebase Console > Project Settings > Service Accounts.",
                MessageType.Info);

            EditorGUILayout.Space(8);

            // 1. Cấu hình Service Account Key
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("1. Cấu Hình Service Account Key", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _serviceAccountKeyPath = EditorGUILayout.TextField("Key JSON Path:", _serviceAccountKeyPath);
            if (GUILayout.Button("Browse...", GUILayout.Width(75)))
            {
                string selected = EditorUtility.OpenFilePanel("Chọn Service Account Key JSON", Application.dataPath, "json");
                if (!string.IsNullOrEmpty(selected))
                {
                    _serviceAccountKeyPath = selected;
                    EditorPrefs.SetString(PREF_KEY_PATH, _serviceAccountKeyPath);
                    ExtractProjectId();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            _projectId = EditorGUILayout.TextField("Project ID:", _projectId);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString(PREF_PROJECT_ID, _projectId);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // 2. Cấu hình Xuất File
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("2. Đường Dẫn File Xuất Trong Unity", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            _targetResourcePath = EditorGUILayout.TextField("Export File:", _targetResourcePath);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString(PREF_EXPORT_PATH, _targetResourcePath);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Set Default (RemoteConfig.json)"))
            {
                _targetResourcePath = "Assets/Resources/RemoteConfig.json";
                EditorPrefs.SetString(PREF_EXPORT_PATH, _targetResourcePath);
            }
            if (GUILayout.Button("Set Android (RemoteConfig_Android.json)"))
            {
                _targetResourcePath = "Assets/Resources/RemoteConfig_Android.json";
                EditorPrefs.SetString(PREF_EXPORT_PATH, _targetResourcePath);
            }
            if (GUILayout.Button("Set iOS (RemoteConfig_iOS.json)"))
            {
                _targetResourcePath = "Assets/Resources/RemoteConfig_iOS.json";
                EditorPrefs.SetString(PREF_EXPORT_PATH, _targetResourcePath);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            // Status message
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
            }

            // Sync Button
            GUI.enabled = !_isSyncing && !string.IsNullOrEmpty(_serviceAccountKeyPath) && !string.IsNullOrEmpty(_projectId);
            if (GUILayout.Button(_isSyncing ? "Đang đồng bộ từ Google Cloud..." : "📥 Tải & Đồng Bộ Remote Config Ngay", GUILayout.Height(40)))
            {
                SyncRemoteConfigAsync();
            }
            GUI.enabled = true;

            EditorGUILayout.Space(8);

            // Preview
            if (!string.IsNullOrEmpty(_previewJson))
            {
                GUILayout.Label("Dữ liệu Remote Config vừa tải:", EditorStyles.boldLabel);
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(180));
                EditorGUILayout.TextArea(_previewJson, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
        }

        private void ExtractProjectId()
        {
            if (string.IsNullOrEmpty(_serviceAccountKeyPath) || !File.Exists(_serviceAccountKeyPath)) return;

            try
            {
                string content = File.ReadAllText(_serviceAccountKeyPath);
                int idx = content.IndexOf("\"project_id\"", StringComparison.Ordinal);
                if (idx >= 0)
                {
                    int colon = content.IndexOf(':', idx);
                    int firstQuote = content.IndexOf('"', colon + 1);
                    int secondQuote = content.IndexOf('"', firstQuote + 1);
                    if (firstQuote >= 0 && secondQuote > firstQuote)
                    {
                        _projectId = content.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                        EditorPrefs.SetString(PREF_PROJECT_ID, _projectId);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FirebaseSync] Không thể tự động trích xuất Project ID: {ex.Message}");
            }
        }

        private async void SyncRemoteConfigAsync()
        {
            _isSyncing = true;
            _statusMessage = "Đang xác thực OAuth2 với Google APIs...";
            _statusType = MessageType.Info;
            Repaint();

            try
            {
                if (!File.Exists(_serviceAccountKeyPath))
                {
                    SetStatus($"Không tìm thấy file Key: {_serviceAccountKeyPath}", MessageType.Error);
                    return;
                }

                // 1. Lấy Access Token từ Service Account Key
                var credential = (ITokenAccess)GoogleCredential.FromFile(_serviceAccountKeyPath)
                    .CreateScoped("https://www.googleapis.com/auth/firebase.remoteconfig");

                string accessToken = await credential.GetAccessTokenForRequestAsync(null, default(CancellationToken));
                if (string.IsNullOrEmpty(accessToken))
                {
                    SetStatus("Không thể tạo OAuth2 Access Token từ Service Account Key!", MessageType.Error);
                    return;
                }

                SetStatus("Đang gọi REST API Firebase Remote Config...", MessageType.Info);
                Repaint();

                // 2. Gọi REST API Firebase Remote Config
                string url = $"https://firebaseremoteconfig.googleapis.com/v1/projects/{_projectId}/remoteConfig";
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await client.GetAsync(url);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    SetStatus($"Lỗi HTTP {response.StatusCode}: {responseBody}", MessageType.Error);
                    return;
                }

                // 3. Trích xuất tham số parameters sang JSON phẳng
                string parsedJson = ParseRemoteConfigTemplate(responseBody);
                _previewJson = parsedJson;

                // 4. Lưu ra file Resources
                string fullPath = Path.Combine(Application.dataPath, "..", _targetResourcePath);
                string dir = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(fullPath, parsedJson);
                AssetDatabase.Refresh();

                SetStatus($"✅ Đã đồng bộ thành công về '{_targetResourcePath}'!", MessageType.Info);
            }
            catch (Exception ex)
            {
                SetStatus($"❌ Lỗi đồng bộ: {ex.Message}", MessageType.Error);
                Debug.LogException(ex);
            }
            finally
            {
                _isSyncing = false;
                Repaint();
            }
        }

        private string ParseRemoteConfigTemplate(string templateJson)
        {
            var resultDict = new Dictionary<string, string>();

            try
            {
                // Parser nhẹ cấu trúc parameters của Firebase Remote Config REST v1
                int paramIdx = templateJson.IndexOf("\"parameters\"", StringComparison.Ordinal);
                if (paramIdx >= 0)
                {
                    int openBrace = templateJson.IndexOf('{', paramIdx);
                    int closeBrace = FindMatchingBrace(templateJson, openBrace);
                    if (openBrace >= 0 && closeBrace > openBrace)
                    {
                        string paramsBlock = templateJson.Substring(openBrace + 1, closeBrace - openBrace - 1);
                        ExtractParamsFromBlock(paramsBlock, resultDict);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FirebaseSync] Parse block warning: {ex.Message}");
            }

            // Tạo chuỗi JSON sạch
            var sb = new StringBuilder("{\n");
            int count = 0;
            foreach (var kvp in resultDict)
            {
                sb.Append($"  \"{kvp.Key}\": \"{kvp.Value.Replace("\"", "\\\"")}\"");
                if (++count < resultDict.Count) sb.Append(",");
                sb.Append("\n");
            }
            sb.Append("}");
            return sb.ToString();
        }

        private void ExtractParamsFromBlock(string block, Dictionary<string, string> result)
        {
            int cur = 0;
            while (cur < block.Length)
            {
                int quoteStart = block.IndexOf('"', cur);
                if (quoteStart < 0) break;
                int quoteEnd = block.IndexOf('"', quoteStart + 1);
                if (quoteEnd < 0) break;

                string paramName = block.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                
                int valIdx = block.IndexOf("\"value\"", quoteEnd);
                if (valIdx >= 0)
                {
                    int colon = block.IndexOf(':', valIdx);
                    int vQuote1 = block.IndexOf('"', colon + 1);
                    int vQuote2 = block.IndexOf('"', vQuote1 + 1);
                    if (vQuote1 >= 0 && vQuote2 > vQuote1)
                    {
                        string val = block.Substring(vQuote1 + 1, vQuote2 - vQuote1 - 1);
                        result[paramName] = val;
                        cur = vQuote2 + 1;
                        continue;
                    }
                }

                cur = quoteEnd + 1;
            }
        }

        private int FindMatchingBrace(string text, int openIdx)
        {
            if (openIdx < 0 || openIdx >= text.Length) return -1;
            int depth = 1;
            for (int i = openIdx + 1; i < text.Length; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }

        private void SetStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
        }
    }
}
