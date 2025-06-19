//AnimatorExtensions.cs
using UnityEngine;

public static class AnimatorExtensions
{
    public static bool HasParameterOfType(this Animator animator, string name, AnimatorControllerParameterType type)
    {
        foreach (var param in animator.parameters)
            if (param.name == name && param.type == type)
                return true;
        return false;
    }
}