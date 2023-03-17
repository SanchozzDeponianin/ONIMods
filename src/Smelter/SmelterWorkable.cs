using UnityEngine;

namespace Smelter
{
    // старая анимация смещена. придется вернуть обработку смещения
    public class SmelterWorkable : ComplexFabricatorWorkable
    {
        public override Vector3 GetWorkOffset() => Vector3.left;
    }
}
