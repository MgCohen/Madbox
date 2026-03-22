using Madbox.LiveOps;
using Madbox.LiveOps.DTO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    public LiveOpsService liveOps;

    // Start is called before the first frame update
    void Start()
    {
        liveOps.PingAsync(new PingRequest { Value = 1 });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
