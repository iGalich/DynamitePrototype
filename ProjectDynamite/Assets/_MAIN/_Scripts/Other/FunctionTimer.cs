using System;
using System.Collections.Generic;
using UnityEngine;

namespace Galich.Other
{
    public class FunctionTimer
    {
        private static List<FunctionTimer> _activeTimerList;
        private static GameObject _initializedGameObject;

        private static void InitializeIfNeeded()
        {
            if (_initializedGameObject == null)
            {
                _initializedGameObject = new GameObject("FunctionTimer_InitGameObject");
                _activeTimerList = new List<FunctionTimer>();
            }
        }

        //public static float GetRemainingTime(string name)
        //{
        //    foreach (FunctionTimer timer in _activeTimerList)
        //    {
        //        if (timer.Name == name)
        //        {
        //            return timer.Time;
        //        }
        //    }
        //    Debug.LogError($"Timer wasn't found.");
        //    return -1f;
        //}

        public static FunctionTimer Create(Action action, float timer, string timerName = null)
        {
            InitializeIfNeeded();

            GameObject gameObject = new("Function Timer", typeof(MonoBehaviourHook));

            FunctionTimer functionTimer = new(action, timer, timerName, gameObject);

            gameObject.GetComponent<MonoBehaviourHook>().OnUpdate = functionTimer.Update;

            _activeTimerList.Add(functionTimer);

            return functionTimer;
        }

        private static void RemoveTimer(FunctionTimer functionTimer)
        {
            InitializeIfNeeded();
            _activeTimerList.Remove(functionTimer);
        }

        public static void StopTimer(string timerName)
        {
            for (int i = 0; i < _activeTimerList.Count; i++)
            {
                if (_activeTimerList[i]._timerName == timerName)
                {
                    _activeTimerList[i].DestroySelf();
                    i--;
                }
            }
        }

        // Dummy class to have access to MonoBehaviour functions
        public class MonoBehaviourHook : MonoBehaviour
        {
            public Action OnUpdate;

            private void Update()
            {
                OnUpdate?.Invoke();
            }
        }

        private Action _action;
        private float _timer;
        private string _timerName;
        private GameObject _gameObject;
        private bool _isDestroyed;

        public string Name => _timerName;
        public float TimeRemaining => _timer;

        private FunctionTimer(Action action, float timer, string timerName, GameObject gameObject, bool useRealTime = false)
        {
            _action = action;
            _timer = timer;
            _timerName = timerName;
            _gameObject = gameObject;
            _isDestroyed = false;
        }

        public void Update()
        {
            if (!_isDestroyed)
            {
                _timer -= Time.deltaTime;
                if (_timer < 0)
                {
                    _action();
                    DestroySelf();
                }
            }
        }

        private void DestroySelf()
        {
            _isDestroyed = true;
            UnityEngine.Object.Destroy(_gameObject);
            RemoveTimer(this);
        }
    }
}