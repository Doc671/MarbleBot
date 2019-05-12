namespace MarbleBot.BaseClasses
{
    /// <summary> The status effects a marble can have during a Siege game. </summary>
    public enum MSE
    {
        /// <summary> The marble is not affected by any status ailment. </summary>
        None,
        /// <summary> The marble will die 45 seconds after this effect is inflicted. </summary>
        Doom,
        /// <summary> The marble's damage output is halved. </summary>
        Chill,
        /// <summary> The marble cannot attack for 15 seconds after this effect is inflicted. </summary>
        Stun,
        /// <summary> The marble will take damage every 15 seconds until reaching 1 HP. </summary>
        Poison
    }
}