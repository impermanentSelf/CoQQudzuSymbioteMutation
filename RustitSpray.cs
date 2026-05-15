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
                return ID == InventoryActionEvent.ID;
            }

            return true;
        }

        public override bool HandleEvent(InventoryActionEvent E) {
            if (E.Command == "Apply") {
                if (!E.Actor.CheckFrozen(Telepathic: false, Telekinetic: true)) {
                    return false;
                }

                if (E.Item.IsBroken() || E.Item.IsRusted()) {
                    E.Actor.ShowFailure("The sprayer head won't move.");
                    return false;
                }

                List<GameObject> inventoryAndEquipment = E.Actor.GetInventoryAndEquipment();
                ProcessCellForTargets(E.Actor, E.Actor.CurrentCell, inventoryAndEquipment);
                foreach (Cell localAdjacentCell in E.Actor.CurrentCell.GetLocalAdjacentCells()) {
                    ProcessCellForTargets(E.Actor, localAdjacentCell, inventoryAndEquipment);
                }

                GameObject gameObject = PickItem.ShowPicker(inventoryAndEquipment, null, PickItem.PickItemDialogStyle.SelectItemDialog, E.Actor, null, null, null, PreserveOrder: false, null, ShowContext: true);
                if (gameObject == null) {
                    return false;
                }

                if (!gameObject.HasPart<Metal>() || !gameObject.FireEvent("ApplyRusted")) {
                    Popup.Show("This item cannot be {{r|rusted}}");
                    return false;
                }

                string Message = null;
                int matterPhase = gameObject.GetMatterPhase();
                if (matterPhase >= 3 || !gameObject.PhaseMatches(E.Actor)) {
                    if (E.Actor.IsPlayer()) {
                        Popup.Show("Pure {{r|rust} coats " + gameObject.t() + ".");
                    }
                }

                else if (matterPhase == 2) {
                    if (E.Actor.IsPlayer()) {
                        Popup.Show("Some {{r|rust}} mixes in with " + gameObject.t() + ".");
                    }

                    gameObject.LiquidVolume?.MixWith(new LiquidVolume("salt", 1));
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

                    gameObject.ApplyEffect(new Rusted());
                }
                ParentObject.Destroy();
                if (!Message.IsNullOrEmpty()) {
                    Popup.Show(Message);
                }
            }

            return base.HandleEvent(E);
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
                    Objects.Add(gameObject);
                }
            }
        }
    }
}
