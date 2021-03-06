﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeoutScreen : BaseScreen
{
    protected override void Awake()
    {
        base.Awake();

        Events.OnGameTimeout += OnGameTimeout;
    }

    private void OnDestroy()
    {
        Events.OnGameTimeout -= OnGameTimeout;
    }

    void OnGameTimeout()
    {
        ToggleScreen(true, null);
    }

    public void PlayAgain()
    {
        Events.CallResetGame?.Invoke();

        ToggleScreen(false, () => Events.CallStartGame?.Invoke());
    }
}