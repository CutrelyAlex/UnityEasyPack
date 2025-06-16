using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPGPack
{
    public class Buff
    {
        public BuffData BuffData { get; set; }
        public GameObject Creator;
        public GameObject Target;
        public float DurationTimer;
        public float TriggerTimer;
        public int CurrentStacks { get; set; } = 1;


        public Action<Buff> OnCreate { get; set; }
        public Action<Buff> OnRemove { get; set; }
        public Action<Buff> OnAddStack { get; set; }
        public Action<Buff> OnReduceStack { get; set; }
        public Action<Buff> OnUpdate { get; set; }
        public Action<Buff> OnTrigger { get; set; }
    }
}