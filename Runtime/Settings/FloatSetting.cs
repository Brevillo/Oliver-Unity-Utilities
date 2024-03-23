using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OliverBeebe.UnityUtilities.Runtime.Settings {

    [CreateAssetMenu(menuName = "Oliver Utilities/Settings/Float")]
    public class FloatSetting : Setting<float> {

        protected override float ToFloat(float value) => value;

        protected override float ToValue(float value) => value;
    }
}
