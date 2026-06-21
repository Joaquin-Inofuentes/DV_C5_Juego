using UnityEditor;
using UnityEngine;

namespace Redes.EditorTools
{
    public static class TriggerCorregir
    {
        [MenuItem("Tools/Redes/Trigger Corregir Now")]
        public static void Run()
        {
            RedesSceneCreator.Corregir();
        }
    }
}