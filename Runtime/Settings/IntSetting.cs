using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OliverBeebe.UnityUtilities.Runtime.Settings {

    [CreateAssetMenu(menuName = "Oliver Utilities/Settings/Int")]
    public class IntSetting : Setting<int> {

        protected override float ToFloat(int value) => value;

        protected override int ToValue(float value) => (int)value;
    }
}
