using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OliverBeebe.UnityUtilities.Runtime.Settings {

    [CreateAssetMenu(menuName = "Oliver Utilities/Settings/Bool")]
    public class BoolSetting : Setting<bool> {

        protected override float ToFloat(bool value) => value ? 1 : 0;

        protected override bool ToValue(float value) => value != 0;
    }
}
