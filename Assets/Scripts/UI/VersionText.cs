using UnityEngine;
using UnityEngine.UI;

using Utils;

namespace UI
{
    [RequireComponent(typeof(Text))]
    public class VersionText : MonoBehaviour
    {
        public Text TextComponent;

        public void Start()
        {
            TextComponent.text = string.Format("Version {0} ({1})",
                Utilities.VersionName,
                Utilities.BuildType);
        }
    }
}
