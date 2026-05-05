using System.Collections.Generic;
using Project.Character;
using Project.Infrastructure;
using UnityEngine;

namespace Project.App
{
    public class CharacterPackageDebugTester : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log($"[CharacterPackageDebugTester] CharactersPath: {RuntimePathInitializer.CharactersPath}");

            var scanner = new CharacterPackageScanner();
            List<CharacterPackageInfo> packages = scanner.Scan();

            Debug.Log($"[CharacterPackageDebugTester] 扫描到角色包数量：{packages.Count}");

            var validator = new CharacterPackageValidator();
            var loader = new CharacterPackageLoader();

            foreach (CharacterPackageInfo packageInfo in packages)
            {
                CharacterValidationResult validationResult = validator.Validate(packageInfo);
                Debug.Log(validationResult.ToString());

                if (!validationResult.Valid)
                {
                    continue;
                }

                CharacterPackageData data;
                string errorMessage;

                bool loaded = loader.TryLoad(packageInfo, out data, out errorMessage);

                if (!loaded)
                {
                    Debug.LogError($"[CharacterPackageDebugTester] 加载失败：{errorMessage}");
                    continue;
                }

                Debug.Log($"[CharacterPackageDebugTester] 加载角色成功：{data.CharacterId} / {data.CharacterName}");
                Debug.Log($"[CharacterPackageDebugTester] Persona 字符数：{data.PersonaText.Length}");
                Debug.Log($"[CharacterPackageDebugTester] Model 路径：{data.Model3JsonAbsolutePath}");
            }
        }
    }
}