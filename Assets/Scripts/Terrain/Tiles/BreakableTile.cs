﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Uncooked.Terrain.Tools;

namespace Uncooked.Terrain.Tiles
{
    public class BreakableTile : Tile, IDamageable
    {
        [SerializeField] private Tile lowerTier;
        [SerializeField] private ParticleSystem breakParticlePrefab;
        [SerializeField] private bool IsUnmineable;

        private Gradient meshColors;

        protected virtual void Start()
        {
            var mats = new List<Material>();
            var gradientPins = new List<GradientColorKey>();

            foreach (Transform t in transform)
            {
                var renderer = t.GetComponent<MeshRenderer>();
                if (renderer) mats.Add(renderer.material);
            }
            for (int i = 0; i < mats.Count; i++)
            {
                gradientPins.Add(new GradientColorKey(mats[i].color, (i + 1f) / mats.Count));
            }

            meshColors = new Gradient();
            meshColors.mode = GradientMode.Fixed;
            meshColors.colorKeys = gradientPins.ToArray();
        }

        public bool TryInteractUsing(IPickupable item, RaycastHit hitInfo)
        {
            if (IsUnmineable) return false;

            if (item is BreakTool breaker && name.Contains(breaker.BreakTileCode)) TakeHit(breaker.Tier, hitInfo);
            else return false;

            return true;
        }

        /// <summary>
        /// Replaces this with it's lowerTier up to given damage number of times
        /// </summary>
        /// <param name="damage">Max amount of times Tile toSpawn will get its lowerTier</param>
        /// <param name="hit">Info about the Raycast used to find this</param>
        public void TakeHit(int damage, RaycastHit hit)
        {
            Tile toSpawn = this;

            for (int i = 0; i < damage; i++)
            {
                if (toSpawn is BreakableTile t)
                {
                    toSpawn = t.lowerTier;

                    var particles = Instantiate(breakParticlePrefab, hit.transform.position, breakParticlePrefab.transform.rotation);
                    Destroy(particles.gameObject, breakParticlePrefab.main.startLifetime.constant);

                    var settings = particles.main;
                    var colors = new ParticleSystem.MinMaxGradient(meshColors);
                    colors.mode = ParticleSystemGradientMode.RandomColor;
                    settings.startColor = colors;
                }
                else break;
            }

            if (toSpawn == this) return;

            Instantiate(toSpawn, transform.position, transform.rotation, transform.parent);
            Destroy(gameObject);
        }
    }
}