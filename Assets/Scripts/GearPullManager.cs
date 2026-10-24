using UnityEngine;
using System.Collections.Generic;
using System;
public class GearPullManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject _gearPullPanel;
    [SerializeField] private GearRewardCard[] _cards;
    [SerializeField] private bool _pauseGameplay = true;
    [Header("Rewards")]
    [SerializeField] private GearReward[] _rewardPool;
    [SerializeField] private GearLoadout _demoLoadout;



    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
