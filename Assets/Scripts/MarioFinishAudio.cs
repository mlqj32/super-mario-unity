using UnityEngine;

namespace Mario
{
    public sealed partial class MarioGame
    {
        AudioSource courseJingle;
        int clearAudioFrames;
        bool auditAudioPlayback;
        bool AudioPlaybackEnabled => !validationMode||auditAudioPlayback;
        void InitializeCourseAudio()
        {
            courseJingle=gameObject.AddComponent<AudioSource>();
            courseJingle.playOnAwake=false;courseJingle.loop=false;courseJingle.volume=.65f;courseJingle.spatialBlend=0;
        }
        AudioClip PlayCourseJingle(string name)
        {
            courseJingle.Stop();courseJingle.pitch=1;
            courseJingle.clip=Resources.Load<AudioClip>("Audio/"+name);
            if(AudioPlaybackEnabled&&courseJingle.clip!=null)courseJingle.Play();
            return courseJingle.clip;
        }
        void BeginClearSequence(string name)
        {
            phase=GamePhase.Clear;phaseFrames=0;music.Stop();sounds.Stop();
            BeginFinishWalk();
            var clip=PlayCourseJingle(name);
            // Keep simulation deterministic even with muted/disabled audio.
            // Live playback is also checked below: fast simulation or a delayed
            // audio start must never cut off a still-playing jingle.
            clearAudioFrames=clip==null?0:Mathf.CeilToInt(clip.length*(float)ClassicMotor.Frequency);
        }
        bool ClearSequenceReady => FinishEntranceReady&&phaseFrames>210&&phaseFrames>=clearAudioFrames&&timeLeft==0&&!finishPending&&
            (!AudioPlaybackEnabled||courseJingle==null||!courseJingle.isPlaying);
        void StopCourseAudio()
        {
            music?.Stop();sounds?.Stop();
            if(courseJingle!=null){courseJingle.Stop();courseJingle.clip=null;}
            clearAudioFrames=0;
        }
        void BeginVictory()
        {StopCourseAudio();phase=GamePhase.Victory;PlayCourseJingle("Ending");}
    }
}
