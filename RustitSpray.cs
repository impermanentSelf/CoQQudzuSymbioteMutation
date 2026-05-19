using System;
using System.Collections.Generic;
using XRL;
using XRL.UI;
using XRL.World;
using XRL.World.Effects;
using XRL.World.Parts;

namespace XRL.World.Parts {
    [Serializable]
    public class RustitSpray : IPart {

        public override bool SameAs(IPart p) {
            return true;
        }

        public override bool WantEvent(int ID, int cascade) {
            if (!base.WantEvent(ID, cascade)) {
                return ID == InventoryActionEvent.ID || ID == GetInventoryActionsAlwaysEvent.ID;
            }

            return true;
        }

        public override bool HandleEvent(GetInventoryActionsAlwaysEvent E) {

            if (!E.Object.IsBroken() && !E.Object.IsRusted()) {
                int @default = 0;

                if (E.Object.Equipped == E.Actor || E.Object.InInventory == E.Actor) {
                    if (E.Object.IsImportant()) {
                        @default = -1;
                    }
                }

                E.AddAction("Apply", "apply", "Apply", null, 'a', FireOnActor: false, @default);
                if (!E.Actor.OnWorldMap()) {
                    E.AddAction("Apply To", "apply to", "ApplyTo", null, 'a', FireOnActor: false, -2);
                }
            }

            return base.HandleEvent(E);
        }

        public override bool HandleEvent(InventoryActionEvent E) {
            if (E.Command == "Apply" || E.Command == "ApplyTo") {
                if (!E.Actor.CheckFrozen(Telepathic: false, Telekinetic: true)) {
                    return false;
                }

                if (E.Item.IsBroken() || E.Item.IsRusted()) {
                    E.Actor.ShowFailure("The sprayer head won't move.");
                    return false;
                }

                //GameObject.GetEquippedObjects() get just the equipment

                List<GameObject> inventoryAndEquipment = new List<GameObject>();

                if (E.Command == "ApplyTo") {
                    if (E.Actor.OnWorldMap()) {
                        return E.Actor.Fail("You cannot do that on the world map.");
                    }

                    Cell cell = PickDirection(ForAttack: false, POV: E.Actor, Label: "Apply " + E.Item.GetDisplayName(int.MaxValue, null, null, AsIfKnown: false, Single: true));
                    ProcessCellForTargets(E.Actor, cell, inventoryAndEquipment);
                }

                else {
                    foreach (GameObject Object in E.Actor.GetInventoryAndEquipment()) {
                        if (CanApply(Object)) {

                            inventoryAndEquipment.Add(Object);
                        }
                    }

                    if (CanApply(E.Actor)) {
                        inventoryAndEquipment.Add(E.Actor);
                    }
                }

                if (inventoryAndEquipment.Count <= 0) {
                    if (E.Actor.IsPlayer()) {
                        if (E.Command == "ApplyTo") {
                            Popup.Show("There is nothing there you can {{r|rust}}.");
                        } else {
                            Popup.Show("There is nothing you can {{r|rust}}.");
                        }
                        return false;

                    }
                }

                GameObject gameObject = PickItem.ShowPicker(inventoryAndEquipment, null, PickItem.PickItemDialogStyle.SelectItemDialog, E.Actor, null, null, null, PreserveOrder: false, null, ShowContext: true);
                if (gameObject == null) {
                    return false;
                }

                string Message = null;
                int matterPhase = gameObject.GetMatterPhase();
                if (matterPhase >= 3 || !gameObject.PhaseMatches(E.Actor)) {
                    if (E.Actor.IsPlayer()) {
                        Popup.Show("Pure {{r|rust} coats " + gameObject.t() + ".");
                    }
                }

                else {
                    E.Actor.PlayWorldOrUISound("Sounds/StatusEffects/sfx_statusEffect_rusted");
                    if (E.Actor.IsPlayer()) {
                        if (gameObject == E.Actor) {
                            Popup.Show("You are covered in {{r|rust}}!");
                        }

                        else {
                            Popup.Show(gameObject.Does("are") + " covered in {{r|rust}}!");
                        }

                        ParentObject.MakeUnderstood(out Message);
                    }

                    bool cursed = gameObject.HasPart<Cursed>();

                    if (cursed) {
                        gameObject.RemovePart<Cursed>(); //need to trick the game into successfully removing Gentling Cones/Masks when they break
                    }

                    if (gameObject.HasTagOrProperty("RustSprayBreakable")) {
                        gameObject.ApplyEffect(new Broken());
                    }

                    else {
                        gameObject.ApplyEffect(new Rusted());
                    }

                    if (cursed) {
                        gameObject.RequirePart<Cursed>();
                    }
                }
                ParentObject.Destroy();
                if (!Message.IsNullOrEmpty()) {
                    Popup.Show(Message);
                }
            }

            return base.HandleEvent(E);
        }

        private bool CanApply(GameObject Object) {
            return (Object.HasPart<Metal>() || Object.HasTagOrProperty("RustSprayBreakable"));
        }

        private void ProcessCellForTargets(GameObject Actor, Cell C, List<GameObject> Objects) {
            if (C == null) {
                return;
            }

            IList<GameObject> list;
            if (!C.IsSolidFor(Actor)) {
                IList<GameObject> objects = C.Objects;
                list = objects;
            }

            else {
                IList<GameObject> objects = C.GetCanInteractInCellWithSolidObjectsFor(Actor);
                list = objects;
            }

            IList<GameObject> list2 = list;
            int i = 0;
            for (int count = list2.Count; i < count; i++) {
                GameObject gameObject = list2[i];
                Physics physics = gameObject.Physics;
                if (physics != null && physics.IsReal && IComponent<GameObject>.Visible(gameObject) && gameObject.Render != null && gameObject.Render.RenderLayer > 0 && !Objects.Contains(gameObject) && gameObject != ParentObject) {
                    if (CanApply(gameObject)) {
                        Objects.Add(gameObject);
                    }

                    if (gameObject.Body != null) {
                        foreach (GameObject equippedObject in gameObject.GetEquippedObjects()) {
                            if (CanApply(equippedObject)) {
                                Objects.Add(equippedObject);
                            }
                        }

                    }
                }
            }
        }
    }
}
