﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Train;
using Uncooked.UI;

namespace Uncooked.Managers
{
    public class GameManager : MonoBehaviour
    {
        public event System.Action OnCheckpoint, OnEndCheckpoint;

        [SerializeField] private LayerMask interactMask;
        [SerializeField] [Min(0)] private float baseTrainSpeed = 0.05f, trainSpeedIncrement = 0.05f, speedUpMultiplier = 2, trainInitialDelay = 8;
        [SerializeField] private bool isEditing, isPaused;
        [Header("Buttons")]
        [SerializeField] private TriggerButton checkpointContinueButton;

        public LayerMask InteractMask => interactMask;
        public float TrainSpeed => trainSpeed;
        public bool IsEditing => isEditing;
        public bool IsPaused => isPaused || isEditing;
        public bool TrainIsSpeeding => trainSpeed - (baseTrainSpeed + trainSpeedIncrement * checkpointCount) > 0.0001f; // > operator is inconsistent

        private TrainCar[] cars;
        private float trainSpeed;
        private int checkpointCount;

        public static GameManager instance;

        void Awake()
        {
            if (instance == null) instance = this;
            else throw new System.Exception("Multiple GameManagers Exist");

            checkpointContinueButton.OnClick += ContinueFromCheckpoint;
            checkpointContinueButton.GetComponent<BoxCollider>().enabled = IsEditing;
        }

        void Start()
        {
            cars = FindObjectsOfType<TrainCar>();
            trainSpeed = baseTrainSpeed;

            HUDManager.instance.UpdateSpeedText(trainSpeed.ToString());

            StartTrainWithDelay(trainInitialDelay);
        }

        public void StartTrainWithDelay(float delayTime) => _ = StartCoroutine(StartTrain(delayTime));

        private IEnumerator StartTrain(float delay)
        {
            yield return new WaitForSeconds(delay - 5);

            for (int countDown = 5; countDown > 0; countDown--)
            {
                yield return new WaitWhile(() => isPaused);

                // Debug.Log(countDown);
                yield return new WaitForSeconds(1);
            }

            foreach (var car in cars) if (car.HasRail) car.StartDriving();
        }

        /// <summary>
        /// Gives train temporary speed buff until it reaches the next checkpoint
        /// </summary>
        public void SpeedUp() => trainSpeed = speedUpMultiplier * (baseTrainSpeed + trainSpeedIncrement * checkpointCount);

        public void ReachCheckpoint()
        {
            isEditing = true;
            checkpointCount++;
            CameraManager.instance.TransitionEditMode(true);
            HUDManager.instance.isUpdating = false;

            Vector3 pos = checkpointContinueButton.transform.position;
            checkpointContinueButton.GetComponent<BoxCollider>().enabled = true;
            checkpointContinueButton.transform.position = new Vector3(pos.x, pos.y, CameraManager.instance.FirstTarget.transform.position.z - 1);

            OnCheckpoint?.Invoke();
        }

        public void ContinueFromCheckpoint()
        {
            isEditing = false;
            trainSpeed = baseTrainSpeed + trainSpeedIncrement * checkpointCount;
            CameraManager.instance.TransitionEditMode(false);
            HUDManager.instance.UpdateSpeedText(trainSpeed.ToString());
            HUDManager.instance.isUpdating = true;

            checkpointContinueButton.GetComponent<BoxCollider>().enabled = false;

            OnEndCheckpoint?.Invoke();
        }

        public static void MoveToLayer(Transform root, int layer)
        {
#pragma warning disable IDE0090 // Use 'new(...)'
            Stack<Transform> moveTargets = new Stack<Transform>();
#pragma warning restore IDE0090 // Use 'new(...)'
            moveTargets.Push(root);

            Transform currentTarget;
            while (moveTargets.Count != 0)
            {
                currentTarget = moveTargets.Pop();
                currentTarget.gameObject.layer = layer;
                foreach (Transform child in currentTarget)
                    moveTargets.Push(child);
            }
        }

        private void OnDestroy()
        {
            instance = null;
            
            checkpointContinueButton.OnClick -= ContinueFromCheckpoint;
        }
    }
}