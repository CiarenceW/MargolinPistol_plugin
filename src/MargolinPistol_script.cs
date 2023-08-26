using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Receiver2ModdingKit;
using Receiver2;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using SimpleJSON;
using System.Security.Cryptography;

namespace MargolinPistol_plugin
{
    public class MargolinPistol_script : ModGunScript
    {
        private readonly float[] slide_push_hammer_curve = new float[] {
            0.03f,
            0,
            0.123f,
            1
        };
        private bool decocking;
        private bool hammer_rest;
        private RotateMover sear;
        private float sear_cocked;
        private float sear_halfcocked;
        private float sear_uncocked;
        private float sear_almost_cocked;
        private float sear_hammer_back;
        private float safety_mid;
        private Transform transform_hammer_strut;
        private Vector3 hammer_strut_rel_pos;
        private Quaternion hammer_strut_rel_rot;
        private Vector3 mainspring_dir;
        private Vector3 hammer_strut_spring_pos;
        private SpringCompressInstance mainspring;

        private float hammer_strut_max_angle = 61.29578f;

        public override void InitializeGun()
        {
            //var round = (from e in ReceiverCoreScript.Instance().generic_prefabs where e.TryGetComponent<ShellCasingScript>(out var shellCasingScript) && shellCasingScript.cartridge_type == CartridgeSpec.Preset._22_LR select e.GetComponent<ShellCasingScript>()).FirstOrDefault();
            //round.go_casing.transform.gameObject.SetActive(false);
            //case_mesh.transform.parent = round.transform;
            //round.go_casing = case_empty_mesh;
            //round.go_round.transform.gameObject.SetActive(false);
            //case_empty_mesh.transform.parent = round.transform;
            //round.go_round = case_mesh;

            var RCS = ReceiverCoreScript.Instance();

            var glint_material = RCS.GetMagazinePrefab("wolfire.glock_17", MagazineClass.StandardCapacity).glint_renderer.material;
            RCS.TryGetMagazinePrefabFromRoot("margolin_sport", MagazineClass.LowCapacity, out var magPrefabLow);
            magPrefabLow.glint_renderer.material = glint_material;
            RCS.TryGetMagazinePrefabFromRoot("margolin_sport", MagazineClass.StandardCapacity, out var magPrefabStd);
            magPrefabStd.glint_renderer.material = glint_material;
        }

        public override void AwakeGun()
        {
            hammer.amount = 1f;

            sear = (RotateMover)typeof(GunScript).GetField("sear", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);
            sear_cocked = (float)typeof(GunScript).GetField("sear_cocked", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);
            sear_halfcocked = (float)typeof(GunScript).GetField("sear_halfcocked", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);
            sear_uncocked = (float)typeof(GunScript).GetField("sear_uncocked", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);
            sear_almost_cocked = (float)typeof(GunScript).GetField("sear_almost_cocked", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);
            sear_hammer_back = (float)typeof(GunScript).GetField("sear_hammer_back", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);

            transform_hammer_strut = (Transform)AccessTools.Field(typeof(GunScript), "transform_hammer_strut").GetValue(this);
            hammer_strut_rel_pos = (Vector3)AccessTools.Field(typeof(GunScript), "hammer_strut_rel_pos").GetValue(this);
            hammer_strut_rel_rot = (Quaternion)AccessTools.Field(typeof(GunScript), "hammer_strut_rel_rot").GetValue(this);
            mainspring_dir = (Vector3)AccessTools.Field(typeof(GunScript), "mainspring_dir").GetValue(this);
            hammer_strut_spring_pos = (Vector3)AccessTools.Field(typeof(GunScript), "mainspring_dir").GetValue(this);
            mainspring = (SpringCompressInstance)AccessTools.Field(typeof(GunScript), "mainspring").GetValue(this);

            var angle_safety = Quaternion.Angle(safety.rotations[0], safety.rotations[1]);
            safety_mid = Quaternion.Angle(safety.rotations[0], slide_stop_mid_component.localRotation) / angle_safety;
        }

        public override void UpdateGun()
        {
            if (IsSlideLockedOpen() && magazine.amount > 0f)
            {
                _slide_stop_locked = false;
            }

            // Decocking logic (I'm pretty sure the game does this on its own but it didn't work when I first tried it but when I removed this section of code it still worked idk why I just hate it here man for fuck's sake)
            if (player_input.GetButton(14) && player_input.GetButtonDown(2)) decocking = true;

            hammer.asleep = false;
            if (decocking)
            {
                if (hammer.amount == 0 || !player_input.GetButton(2))
                {
                    _hammer_state = 0;
                    decocking = false;

                    if (hammer.amount == 0f) AudioManager.PlayOneShotAttached(sound_decock, hammer.transform.gameObject, 0.3f);
                }
            }
            if (!decocking)
            {
                if (_hammer_state == 0 && hammer.amount >= _hammer_halfcocked)
                {
                    _hammer_state = 1;
                    if (!hammer.asleep)
                    {
                        hammer.target_amount = _hammer_halfcocked;
                    }
                    if (!ReceiverCoreScript.Instance().player.lah.PullingTrigger) AudioManager.PlayOneShotAttached("event:/guns/1911/1911_half_cock", hammer.transform.gameObject);
                }
                if (_hammer_state == 1 && hammer.amount >= _hammer_cocked_val)
                {
                    _hammer_state = 2;
                    if (!hammer.asleep)
                    {
                        hammer.target_amount = _hammer_cocked_val;
                    }
                    if (!ReceiverCoreScript.Instance().player.lah.PullingTrigger) AudioManager.PlayOneShotAttached("event:/guns/1911/1911_full_cock", hammer.transform.gameObject);
                }
                if ((trigger.amount == 0f && !player_input.GetButton(14)))
                {
                    if (_hammer_state == 1)
                    {
                        hammer.target_amount = _hammer_halfcocked;
                        if (!hammer_rest) AudioManager.PlayOneShotAttached("event:/guns/1911/1911_hammer_rest", this.hammer.transform.gameObject);
                    }
                    if (_hammer_state == 2)
                    {
                        hammer.target_amount = _hammer_cocked_val;
                        if (!hammer_rest) AudioManager.PlayOneShotAttached("event:/guns/1911/1911_hammer_rest", this.hammer.transform.gameObject);
                    }
                    hammer_rest = true;
                }
                else
                {
                    hammer_rest = false;
                }
            }
            transform_hammer_strut.position = hammer.transform.TransformPoint(hammer_strut_rel_pos);
            transform_hammer_strut.rotation = hammer.transform.rotation * hammer_strut_rel_rot;
            Vector3 vector = Quaternion.AngleAxis(90f, transform.right) * transform.rotation * mainspring_dir;
            float num8 = Vector3.Magnitude(transform.TransformVector(hammer_strut_spring_pos));
            float num9 = Vector3.Dot(transform_hammer_strut.TransformPoint(hammer_strut_spring_pos) - transform_hammer_strut.position, vector) / num8;
            float num10 = Vector3.Dot(mainspring.new_top.position - transform_hammer_strut.position, vector) / num8;
            float num11 = Mathf.Asin(num9);
            float num12 = Mathf.Asin(num10);
            transform_hammer_strut.rotation = Quaternion.AngleAxis((num12 - num11) * hammer_strut_max_angle, transform.right) * transform_hammer_strut.rotation;
            float num13 = Vector3.Dot(transform.InverseTransformPoint(mainspring.new_top.position), mainspring_dir);
            float num14 = Vector3.Dot(transform.InverseTransformPoint(transform_hammer_strut.TransformPoint(hammer_strut_spring_pos)), mainspring_dir);
            hammer.TimeStep(Time.deltaTime * 5);

            if (IsSafetyOn())
            {
                if (_hammer_state == 2)
                {
                    _hammer_state = 2; //I don't have to do this but this is literally lifted from the M45A1

                    hammer.amount = Mathf.Max(hammer.amount, _hammer_cocked_val);
                    hammer.UpdateDisplay();
                }

                trigger.amount = Mathf.Min(trigger.amount, 0.1f);
                trigger.UpdateDisplay();
            }
            if (slide.amount > 0f)
            {
                _disconnector_needs_reset = true;
            }
            if (_disconnector_needs_reset && slide.amount == 0f && trigger.amount == 0f) //makes it so you have to unpress the trigger to be able to shoot again I think actually I don't know really but it seems like what it is
            {
                _disconnector_needs_reset = false;
                AudioManager.PlayOneShotAttached(sound_trigger_reset, trigger.transform.gameObject);
            }

            if (_hammer_state != 2)
            {
                hammer.amount = Mathf.Max(hammer.amount, InterpCurve(slide_push_hammer_curve, slide.amount));
                hammer.UpdateDisplay();
            }

            if (trigger.amount == 1f && hammer.amount == _hammer_cocked_val && !_disconnector_needs_reset && !IsSafetyOn()) //hammer firing logic
            {
                if (slide.amount == 0f)
                {
                    hammer.target_amount = 0f;
                    hammer.vel = -0.1f * ReceiverCoreScript.Instance().player_stats.animation_speed;
                }
            }
            hammer.TimeStep(Time.deltaTime);
            if (hammer.amount == 0f && _hammer_state == 2 && !decocking) //shooting logic
            {
                TryFireBullet(1);
                _hammer_state = 0;
            }
            if (hammer.amount < _hammer_halfcocked)
            {
                sear.amount = sear_uncocked;
            }
            else if (hammer.amount < _hammer_cocked_val)
            {
                sear.amount = Mathf.Lerp(sear_halfcocked, sear_almost_cocked, (hammer.amount - _hammer_halfcocked) / (_hammer_cocked_val - _hammer_halfcocked));
            }
            else
            {
                sear.amount = Mathf.Lerp(sear_cocked, sear_hammer_back, (hammer.amount - _hammer_cocked_val) / (1f - _hammer_cocked_val));
            }

            if (decocking && slide.amount > 0f)
            {
                _hammer_state = 2;

                hammer.amount = Mathf.Clamp(hammer.amount, _hammer_cocked_val, 1f);
                hammer.UpdateDisplay();
            }
            sear.UpdateDisplay();
            trigger.UpdateDisplay();
            UpdateAnimatedComponents();

            //I write this and then the next day I look at this and I die
            if (IsSafetyOn())
            {
                if (slide.amount > 0.2f)
                {
                    safety.amount = Mathf.MoveTowards(safety.amount, 1f, Time.deltaTime * 10f);
                    safety.UpdateDisplay();
                }
                else
                {
                    safety.amount = Mathf.MoveTowards(safety.amount, safety_mid, Time.deltaTime * 10f);
                }
            }
            else
            {
                safety.amount = Mathf.MoveTowards(safety.amount, 0f, Time.deltaTime * 10f);
            }
            if (safety.amount <= safety_mid)
            {
                safety.UpdateDisplay();
            }
        }
    }
}
