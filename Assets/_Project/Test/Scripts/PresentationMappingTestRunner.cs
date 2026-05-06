using System.IO;
using Project.Infrastructure;
using Project.Presentation;
using UnityEngine;

namespace Project.Test
{
    public class PresentationMappingTestRunner : MonoBehaviour
    {
        private void Start()
        {
            // 这里的 Koharu 改成你当前 persistentDataPath/UserData/Characters 下真实存在的角色文件夹名。
            string characterRootPath = Path.Combine(RuntimePathInitializer.CharactersPath, "Huohuo");

            var resolver = new BehaviorMappingResolver(characterRootPath);
            var queue = new PresentationCommandQueue();

            PresentationResolveResult result = resolver.Resolve(
                emotion: "happy",
                action: "greet",
                voiceStyle: "cheerful"
            );

            queue.EnqueueRange(result.ToCommands());
            queue.ExecuteAllDebug();
        }
    }

}
