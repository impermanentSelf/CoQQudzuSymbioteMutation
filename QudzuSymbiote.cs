using System;
using XRL.World.Parts;

namespace XRL.World.Parts.Mutation {
    [Serializable]
    public class QudzuSymbiote : BaseMutation {
        public override bool CanLevel() {
            return false;
        }

        public override string GetDescription() {
            return string.Concat("You are a Qudzu Symbiote\n\n",
                                 "+300 reputation with {{w|vines}}\n",
                                 "15% chance to {{r|rust}} items on hit");
        }

        public override string GetLevelText(int Level) {
            return "";
        }

        public override bool Mutate(GameObject GO, int level) {
            GO.AddPart(new RustOnHit());
            GO.RequirePart<SocialRoles>().RequireRole("{{r|qudzu}} symbiote");
            GO.RequirePart<QudzuSymbioteIconColor>();
            return base.Mutate(GO, level);
        }

        public override bool Unmutate(GameObject GO) {
            GO.RemovePart<RustOnHit>();
            GO.GetPart<SocialRoles>().RemoveRole("{{r|qudzu}} symbiote");
            GO.RemovePart<QudzuSymbioteIconColor>();
            return base.Unmutate(GO);
        }
    }
}
