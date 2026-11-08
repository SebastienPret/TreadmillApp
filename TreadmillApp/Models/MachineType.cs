namespace TreadmillApp.Models
{
    /// <summary>Type d'appareil FTMS pris en charge par l'application.</summary>
    public enum MachineType
    {
        Treadmill = 0,     // tapis de course (FTMS Treadmill Data, 0x2ACD)
        CrossTrainer = 1   // elliptique (FTMS Cross Trainer Data, 0x2ACE)
    }
}