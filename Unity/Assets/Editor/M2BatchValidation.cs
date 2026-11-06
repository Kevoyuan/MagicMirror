using System;
using UnityEditor;
using UnityEngine;

namespace MagicMirror.Editor
{
    /// <summary>
    /// Run all existing synthetic math/hierarchy checks in a real Unity Editor:
    /// Unity -batchmode -nographics -quit -projectPath Unity
    ///       -executeMethod MagicMirror.Editor.M2BatchValidation.Run
    ///
    /// Do not treat this as a webcam, RenderTexture or FBX integration test.
    /// This suite deliberately does NOT invoke interactive scene builders.
    /// </summary>
    public static class M2BatchValidation
    {
        public static void Run()
        {
            Debug.Log("MAGICMIRROR_M2_BATCH_BEGIN");
            string current = "startup";
            try
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                    throw new InvalidOperationException("Editor still compiling/importing.");

                current = "pose math";
                PoseMathSelfCheck.Execute();
                current = "full-body pose math";
                FullBodyPoseSelfCheck.Execute();
                current = "full-body stability and hierarchy";
                FullBodyStabilitySelfCheck.Execute();
                current = "atlas registration and paths";
                AtlasRegistrationSelfCheck.Run();

                Debug.Log("MAGICMIRROR_M2_BATCH_PASS — four synthetic Editor suites. "
                    + "Physical camera, real FBX, visual alignment and FPS NOT verified.");
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogError("MAGICMIRROR_M2_BATCH_FAIL at " + current + ": " + error);
                EditorApplication.Exit(1);
            }
        }
    }
}
