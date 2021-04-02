﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unrailed.Terrain;

namespace Unrailed.Player
{
    public class PlayerController : MonoBehaviour
    {
        public System.Action<bool> OnPickUp, OnDrop; // True for Pickups false for Tools

        public float moveSpeed = 5, turnSpeed = 360, armTurnSpeed = 2, armSwingSpeed = 3;
        public LayerMask interactMask;

        [Header("Transforms")] public Transform toolHolder;
        public Transform pickupHolder, armL, armR;

        private Rigidbody rb;
        private MapManager map;
        private Tile heldObject;
        private bool isSwinging;

        void Start()
        {
            OnPickUp += RaiseArms;
            OnDrop += LowerArms;

            rb = GetComponent<Rigidbody>();
            map = FindObjectOfType<MapManager>();
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position + Vector3.up, transform.forward, out hit, 1, interactMask, QueryTriggerInteraction.Collide))
                {
                    var obj = hit.collider.gameObject.GetComponent<Tile>();

                    if (heldObject == null)
                    {
                        if (obj is IPickupable pickup)
                        {
                            bool isPickup = pickup is PickupTile;

                            pickup.PickUp(isPickup ? pickupHolder : toolHolder);
                            OnPickUp?.Invoke(isPickup);
                            heldObject = obj;
                        }
                    }
                    else
                    {
                        if (!isSwinging && heldObject is Tool tool)
                        {
                            tool.InteractWith(obj, hit.point);
                            StartCoroutine(SwingTool());
                        }
                        else if (heldObject is PickupTile pickup && obj is PickupTile stack)
                        {
                            pickup.TryStackOn(stack);
                            OnDrop?.Invoke(true);
                        }
                    }
                }
                else if (heldObject != null)
                {
                    map.PlaceTile(heldObject, transform.position + Vector3.up + transform.forward);
                    OnDrop?.Invoke(heldObject is PickupTile);
                    heldObject = null;
                }
            }
        }

        void FixedUpdate()
        {
            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

            if (input != Vector3.zero)
            {
                rb.MovePosition(transform.position + moveSpeed * input * Time.deltaTime);

                transform.forward = Vector3.RotateTowards(transform.forward, input, turnSpeed * Mathf.Deg2Rad * Time.deltaTime, 0);
            }
        }

        #region Arm Movement
        private void RaiseArms(bool both)
        {
            StopAllCoroutines();
            StartCoroutine(TurnArm(armR, -90, armTurnSpeed));
            if (both) StartCoroutine(TurnArm(armL, -90, armTurnSpeed));
        }

        private void LowerArms(bool both)
        {
            StopAllCoroutines();
            StartCoroutine(TurnArm(armR, 0, armTurnSpeed));
            if (both) StartCoroutine(TurnArm(armL, 0, armTurnSpeed));
        }

        private IEnumerator SwingTool()
        {
            isSwinging = true;

            yield return StartCoroutine(TurnArm(armR, 0, armSwingSpeed));
            yield return new WaitForSeconds(0.1f);
            yield return StartCoroutine(TurnArm(armR, -90, armTurnSpeed));

            isSwinging = false;
        }

        /// <summary>
        /// Animates arm turning around it's local x
        /// </summary>
        /// <param name="arm">Selected arm</param>
        /// <param name="rotation">Final x value on arm.eulerAngles.x</param>
        /// <param name="speed">Speed of the animation</param>
        private IEnumerator TurnArm(Transform arm, float rotation, float speed)
        {
            Quaternion from = arm.localRotation, to = from * Quaternion.Euler(rotation - from.eulerAngles.x, 0, 0);
            float percent = 0;

            while (percent < 1)
            {
                yield return null;

                percent += speed * Time.deltaTime;
                arm.localRotation = Quaternion.Lerp(from, to, percent);
            }

            arm.localRotation = to;
        }
        #endregion
    }
}