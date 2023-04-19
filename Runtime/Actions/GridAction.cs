using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface GridAction
{
    public void OnStart();

    public void Update();

    public void Cancel();
}
