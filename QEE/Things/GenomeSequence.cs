﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;

namespace QEthics
{
    /// <summary>
    /// Stores relevant information about the genome of a pawn.
    /// </summary>
    public class GenomeSequence : ThingWithComps
    {
        //Relevant for all genomes.
        public string sourceName = "QE_BlankGenomeTemplateName".Translate().RawText ?? "Do Not Use This";
        //public string sourceName = (new TaggedString("QE_BlankGenomeTemplateName")).Translate() ?? "Do Not Use This";
        public PawnKindDef pawnKindDef = PawnKindDefOf.Colonist;
        public Gender gender = Gender.None;

        //Only relevant for humanoids.
        public BodyTypeDef bodyType = BodyTypeDefOf.Thin;
        public CrownType crownType = CrownType.Average;
        public List<ExposedTraitEntry> traits = new List<ExposedTraitEntry>();
        public Color hairColor = new Color(0.0f,0.0f,0.0f);
        public float skinMelanin = 0f;
        public HairDef hair = null;
        public string headGraphicPath = null;
        
        /// <summary>
        /// List containing all hediff def information that should be saved and applied to clones 
        /// </summary>
        public List<HediffInfo> hediffInfos = new List<HediffInfo>();

        //AlienRace compatibility.
        /// <summary>
        /// If true, Alien Race attributes should be shown.
        /// </summary>
        public bool isAlien = false;
        public Color skinColor = new Color();
        public Color skinColorSecond = new Color();
        public Color hairColorSecond = new Color();
        public string crownTypeAlien = "";
        public List<int> addonVariants = new List<int>();

        public override void ExposeData()
        {
            base.ExposeData();

            //Basic.
            Scribe_Values.Look(ref sourceName, "sourceName");
            Scribe_Defs.Look(ref pawnKindDef, "pawnKindDef");
            Scribe_Values.Look(ref gender, "gender");

            //Load first humanoid value
            Scribe_Values.Look(ref crownType, "crownType");

            Scribe_Collections.Look(ref hediffInfos, "hediffInfos", LookMode.Deep);

            //Save/Load rest of the humanoid values. CrownType will be Undefined for animals.
            if (crownType != CrownType.Undefined)
            {
                Scribe_Defs.Look(ref bodyType, "bodyType");
                Scribe_Values.Look(ref hairColor, "hairColor");
                Scribe_Values.Look(ref skinMelanin, "skinMelanin");
                Scribe_Collections.Look(ref traits, "traits", LookMode.Deep);
                Scribe_Defs.Look(ref hair, "hair");
                Scribe_Values.Look(ref headGraphicPath, "headGraphicPath");

                //Humanoid values that could be null in save file go here
                if (Scribe.mode == LoadSaveMode.PostLoadInit)
                {
                    if (hair == null)
                    {
                        //hair = DefDatabase<HairDef>.AllDefs.RandomElement();
                        hair = DefDatabase<HairDef>.GetNamed("Shaved");
                    }
                    if (headGraphicPath == null)
                    {
                        //*slaps roof of car* this bad code can crash your game!
                        //headGraphicPath = GraphicDatabaseHeadRecords.GetHeadRandom(gender, PawnSkinColors.GetSkinColor(skinMelanin), crownType).GraphicPath;

                        headGraphicPath = gender == Gender.Male ? "Things/Pawn/Humanlike/Heads/Male/Male_Average_Normal" :
                            "Things/Pawn/Humanlike/Heads/Female/Female_Narrow_Normal";
                    }
                }
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit && hediffInfos != null)
            {
                //remove any hediffs where the def is missing. Most commonly occurs when a mod is removed from a save.
                int removed = hediffInfos.RemoveAll(h => h.def == null);
                if(removed > 0)
                {
                    QEEMod.TryLog("Removed " + removed + " null hediffs from hediffInfo list for " + sourceName + "'s genome template ");
                }
            }

            //Alien Compat.
            Scribe_Values.Look(ref isAlien, "isAlien");
            Scribe_Values.Look(ref skinColor, "skinColor");
            Scribe_Values.Look(ref skinColorSecond, "skinColorSecond");
            Scribe_Values.Look(ref hairColorSecond, "hairColorSecond");
            Scribe_Values.Look(ref crownTypeAlien, "crownTypeAlien");
            Scribe_Collections.Look(ref addonVariants, "addonVariants");
        }

        public override bool CanStackWith(Thing other)
        {
            if(other is GenomeSequence otherGenome &&
                sourceName == otherGenome.sourceName &&
                pawnKindDef == otherGenome.pawnKindDef &&
                gender == otherGenome.gender &&
                bodyType == otherGenome.bodyType &&
                crownType == otherGenome.crownType &&
                hairColor == otherGenome.hairColor &&
                skinMelanin == otherGenome.skinMelanin &&
                isAlien == otherGenome.isAlien &&
                skinColor == otherGenome.skinColor &&
                skinColorSecond == otherGenome.skinColorSecond &&
                hairColorSecond == otherGenome.hairColorSecond &&
                crownTypeAlien == otherGenome.crownTypeAlien &&
                (hair != null && otherGenome.hair != null && hair.ToString() == otherGenome.hair.ToString()
                    || hair == null && otherGenome.hair == null) &&
                traits.SequenceEqual(otherGenome.traits) &&
                (hediffInfos != null && otherGenome.hediffInfos != null && 
                    hediffInfos.OrderBy(h => h.def.LabelCap).SequenceEqual(otherGenome.hediffInfos.OrderBy(h => h.def.LabelCap))
                    || hediffInfos == null && otherGenome.hediffInfos == null) &&
                (addonVariants != null && otherGenome.addonVariants != null && addonVariants.SequenceEqual(otherGenome.addonVariants)
                    || addonVariants == null && otherGenome.addonVariants == null))
            {
                return base.CanStackWith(other);
            }

            return false;
        }

        /*public bool TraitsAreEqual(IEnumerable<ExposedTraitEntry> otherTraits)
        {
            foreach(var entry in otherTraits)
            {

            }

            return true;
        }*/

        public override Thing SplitOff(int count)
        {
            Thing splitThing = base.SplitOff(count);
            if(splitThing != this && splitThing is GenomeSequence splitThingStack)
            {
                //Basic.
                splitThingStack.sourceName = sourceName;
                splitThingStack.pawnKindDef = pawnKindDef;
                splitThingStack.gender = gender;

                //Humanoid only.
                splitThingStack.bodyType = bodyType;
                splitThingStack.crownType = crownType;
                splitThingStack.hairColor = hairColor;
                splitThingStack.skinMelanin = skinMelanin;
                splitThingStack.hair = hair;
                splitThingStack.headGraphicPath = headGraphicPath;
                foreach (ExposedTraitEntry traitEntry in traits)
                {
                    splitThingStack.traits.Add(new ExposedTraitEntry(traitEntry));
                }

                if (hediffInfos != null)
                {
                    foreach (HediffInfo h in hediffInfos)
                    {
                        splitThingStack.hediffInfos.Add(h);
                    }
                }

                //Alien Compat.
                splitThingStack.isAlien = isAlien;
                splitThingStack.skinColor = skinColor;
                splitThingStack.skinColorSecond = skinColorSecond;
                splitThingStack.hairColorSecond = hairColorSecond;
                splitThingStack.crownTypeAlien = crownTypeAlien;

                if (addonVariants != null)
                {
                    splitThingStack.addonVariants = addonVariants;
                }
            }

            return splitThing;
        }

        public override string LabelNoCount
        {
            get
            {
                if (GetComp<CustomNameComp>() is CustomNameComp nameComp && nameComp.customName.NullOrEmpty())
                {
                    if (sourceName != null)
                    {
                        return sourceName + " " + base.LabelNoCount;
                    }
                    else
                    {
                        return base.LabelNoCount;
                    }
                }

                return base.LabelNoCount;
            }
        }

        public override string DescriptionDetailed
        {
            get
            {
                return CustomDescriptionString(base.DescriptionDetailed);
            }
        }

        public override string DescriptionFlavor
        {
            get
            {
                return CustomDescriptionString(base.DescriptionFlavor);
            }
        }
        /// <summary>
        /// This function checks if a brain template is valid for use in the cloning vat. The game occasionally generates blank
        /// templates from events, or the player can debug one in.
        /// </summary>
        public bool IsValidTemplate()
        {
            if (sourceName != null && sourceName != "QE_BlankGenomeTemplateName".Translate() && pawnKindDef != null)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// This helper function appends the data stored in the genome sequence to the item description in-game.
        /// </summary>
        public string CustomDescriptionString(string baseDescription)
        {
            StringBuilder builder = new StringBuilder();

            if (IsValidTemplate())
            {
                builder.AppendLine(baseDescription);
                builder.AppendLine();
                builder.AppendLine();

                builder.AppendLine("QE_GenomeSequencerDescription_Name".Translate() + ": " + sourceName);
                builder.AppendLine("QE_GenomeSequencerDescription_Race".Translate() + ": " + pawnKindDef.race.LabelCap);
                builder.AppendLine("QE_GenomeSequencerDescription_Gender".Translate() + ": " + GenderUtility.GetLabel(gender, pawnKindDef.race.race.Animal).CapitalizeFirst());

                if (hair != null && hair.texPath != null)
                {
                    builder.AppendLine("QE_GenomeSequencerDescription_Hair".Translate() + ": " + hair.ToString());
                }

                //Traits
                if (traits.Count > 0)
                {
                    builder.AppendLine("QE_GenomeSequencerDescription_Traits".Translate());
                    foreach (ExposedTraitEntry traitEntry in traits)
                    {
                        builder.AppendLine("    " + traitEntry.def.DataAtDegree(traitEntry.degree).label.CapitalizeFirst());
                    }
                }

                //Hediffs
                HediffInfo.GenerateDescForHediffList(ref builder, hediffInfos);
            }
            else
            {
                builder.AppendLine("QE_BlankGenomeTemplateDescription".Translate());
            }

            return builder.ToString().TrimEndNewlines();
        }

        //this changes the text displayed in the bottom-left info panel when you select the item
        public override string GetInspectString()
        {
            StringBuilder builder = new StringBuilder(base.GetInspectString());

            builder.AppendLine("QE_GenomeSequencerDescription_Race".Translate() + ": " + pawnKindDef.race.LabelCap);
            builder.AppendLine("QE_GenomeSequencerDescription_Gender".Translate() + ": " + GenderUtility.GetLabel(gender, pawnKindDef.race.race.Animal).CapitalizeFirst());

            if(hediffInfos != null)
            {
                if (hediffInfos.Count > 0)
                {
                    builder.AppendLine("QE_GenomeSequencerDescription_Hediffs".Translate() + ": " + hediffInfos.Count);
                }
            }

            return builder.ToString().TrimEndNewlines();
        }

    }
}
