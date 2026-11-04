using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NoOpArmy
{
    public class DocumentationMenu
    {
        [UnityEditor.MenuItem("Window/NoOpArmy/Our Website")]
        static void OpenOurSite()
        {
            UnityEngine.Application.OpenURL("https://nooparmygames.com");
        }
        [UnityEditor.MenuItem("Window/NoOpArmy/Documentation/Wise Feline Utility AI")]
        static void OpenDocs()
        {
            UnityEngine.Application.OpenURL("https://nooparmygames.com/WF-UtilityAI-Unity");
        }

        [UnityEditor.MenuItem("Window/NoOpArmy/Documentation/Influence Maps")]
        static void OpenIMapDocs()
        {
            UnityEngine.Application.OpenURL("https://www.nooparmygames.com/WF-UtilityAI-Unity/articles/influencemaps/imapsmodule.html");
        }

        [UnityEditor.MenuItem("Window/NoOpArmy/Documentation/Blackboards")]
        static void OpenBlackboardsDocs()
        {
            UnityEngine.Application.OpenURL("https://www.nooparmygames.com/WF-UtilityAI-Unity/articles/blackboards.html");
        }

        [UnityEditor.MenuItem("Window/NoOpArmy/Documentation/Smart Objects")]
        static void OpenSmartObjectsDocs()
        {
            UnityEngine.Application.OpenURL("https://www.nooparmygames.com/WF-UtilityAI-Unity/articles/smartobjects.html");
        }

        [UnityEditor.MenuItem("Window/NoOpArmy/Documentation/AI Tags")]
        static void OpenAITagsDocs()
        {
            UnityEngine.Application.OpenURL("https://www.nooparmygames.com/WF-UtilityAI-Unity/articles/aitagsmodule.html");
        }

        [UnityEditor.MenuItem("Window/NoOpArmy/Documentation/Remembrance")]
        static void OpenRemembranceDocs()
        {
            UnityEngine.Application.OpenURL("https://www.nooparmygames.com/WF-UtilityAI-Unity/articles/remembrance/overview.html");
        }
    }
}