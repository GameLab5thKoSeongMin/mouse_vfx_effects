// Code responsibility: store one SFX library entry that can be addressed by a string ID.
using System;
using UnityEngine;

[Serializable]
public sealed class SoundEntry
{
    [SerializeField] private string id;
    [SerializeField] private AudioClip clip;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private Vector2 pitchRange = new Vector2(1f, 1f);

    public string Id
    {
        get { return id; }
    }

    public AudioClip Clip
    {
        get { return clip; }
    }

    public float Volume
    {
        get { return volume; }
    }

    public Vector2 PitchRange
    {
        get { return pitchRange; }
    }

    public bool HasValidId
    {
        get { return !string.IsNullOrWhiteSpace(id); }
    }

    public float GetRandomPitch()
    {
        float min = Mathf.Min(pitchRange.x, pitchRange.y);
        float max = Mathf.Max(pitchRange.x, pitchRange.y);
        return Mathf.Approximately(min, max) ? min : UnityEngine.Random.Range(min, max);
    }
}
