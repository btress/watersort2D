using UnityEngine;

public enum SfxType
{
    Click,
    Pour,
    Complete,
    Win,
    Error
}

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;

    [SerializeField] private AudioClip _clickClip;
    [SerializeField] private AudioClip _pourClip;
    [SerializeField] private AudioClip _completeClip;
    [SerializeField] private AudioClip _winClip;
    [SerializeField] private AudioClip _errorClip;
    [SerializeField, Range(0f, 1f)] private float _volume = 1f;

    private AudioSource _source;

    private void Awake()
    {
        _instance = this;
        _source = GetComponent<AudioSource>();
        if (_source == null) _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    public static void Play(SfxType type)
    {
        if (_instance != null) _instance.PlayClip(type);
    }

    private void PlayClip(SfxType type)
    {
        AudioClip clip = type switch
        {
            SfxType.Click => _clickClip,
            SfxType.Pour => _pourClip,
            SfxType.Complete => _completeClip,
            SfxType.Win => _winClip,
            SfxType.Error => _errorClip,
            _ => null
        };

        if (clip != null) _source.PlayOneShot(clip, _volume);
    }
}
