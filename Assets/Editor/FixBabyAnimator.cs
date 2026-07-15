using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.Linq;

[InitializeOnLoad]
public class FixBabyAnimator {
    static FixBabyAnimator() {
        EditorApplication.delayCall += Execute;
    }

    public static void Execute() {
        string path = "Assets/Animation/thanhgiongembe.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null) {
            return;
        }

        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

        // Check if we already have AnyState transitions to avoid running every compilation
        if (rootStateMachine.anyStateTransitions.Length >= 2) {
            return; 
        }

        rootStateMachine.anyStateTransitions = new AnimatorStateTransition[0];

        AnimatorState idleState = rootStateMachine.states.FirstOrDefault(s => s.state.name == "Idle").state;
        AnimatorState walkState = rootStateMachine.states.FirstOrDefault(s => s.state.name == "BabyWalkingAnimation").state;

        if (idleState != null && walkState != null) {
            // AnyState -> Idle
            AnimatorStateTransition toIdle = rootStateMachine.AddAnyStateTransition(idleState);
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isWalking");
            toIdle.duration = 0.1f;
            toIdle.hasFixedDuration = true;
            toIdle.canTransitionToSelf = false;

            // AnyState -> Walk
            AnimatorStateTransition toWalk = rootStateMachine.AddAnyStateTransition(walkState);
            toWalk.AddCondition(AnimatorConditionMode.If, 0, "isWalking");
            toWalk.duration = 0.1f;
            toWalk.hasFixedDuration = true;
            toWalk.canTransitionToSelf = false;

            // Also clear the loop/exit transitions if they are unnecessary, 
            // but setting AnyState transitions is enough to force state changes.

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("Successfully fixed baby animator transitions!");
        }
    }
}
