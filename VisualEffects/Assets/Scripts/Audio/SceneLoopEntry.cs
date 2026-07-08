// Code responsibility: store one scene-to-loop-audio mapping for SoundManager.
using System;
using UnityEngine;

[Serializable]
public sealed class SceneLoopEntry
{
    [SerializeField] private string sceneName;
    [SerializeField] private AudioClip loopClip;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    public string SceneName
    {
        get { return sceneName; }
    }

    public AudioClip LoopClip
    {
        get { return loopClip; }
    }

    public float Volume
    {
        get { return Mathf.Clamp01(volume); }
    }

    public bool HasValidSceneName
    {
        get { return !string.IsNullOrWhiteSpace(sceneName); }
    }

    public bool HasValidLoopClip
    {
        get { return loopClip != null; }
    }
}
