﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using Verse.Sound;
using Verse.AI;

namespace QEthics
{
    /// <summary>
    /// A template of a pawns brain. Stores skills and backstories
    /// </summary>
    public class BrainScanTemplate : ThingWithComps
    {
        public Ideo scannedIdeology = null;
        public string sourceName = null;
        public PawnKindDef kindDef = null;

        //Humanoid only
        public Backstory backStoryChild;
        public Backstory backStoryAdult;
        public List<ComparableSkillRecord> skills = new List<ComparableSkillRecord>();

        /// <summary>
        /// List containing all hediff def information that should be saved and applied to clones 
        /// </summary>
        public List<HediffInfo> hediffInfos = new List<HediffInfo>();

        //Animals only
        public bool isAnimal;
        public DefMap<TrainableDef, bool> trainingLearned = new DefMap<TrainableDef, bool>();
        public DefMap<TrainableDef, int> trainingSteps = new DefMap<TrainableDef, int>();

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref sourceName, "sourceName");
            Scribe_Defs.Look(ref kindDef, "kindDef");

            string childhoodIdentifier = (backStoryChild == null) ? null : backStoryChild.identifier;
            Scribe_Values.Look(ref childhoodIdentifier, "backStoryChild");
            if (Scribe.mode == LoadSaveMode.LoadingVars && !childhoodIdentifier.NullOrEmpty())
            {
                if (!BackstoryDatabase.TryGetWithIdentifier(childhoodIdentifier, out backStoryChild, true))
                {
                    //removed the booleans as it appears to be obsolete
                    Log.Error("Couldn't load child backstory with identifier " + childhoodIdentifier + ". Giving random.");
                    backStoryChild = BackstoryDatabase.RandomBackstory(BackstorySlot.Childhood);
                }
            }

            string adulthoodIdentifier = (backStoryAdult == null) ? null : backStoryAdult.identifier;
            Scribe_Values.Look(ref adulthoodIdentifier, "backStoryAdult");
            if (Scribe.mode == LoadSaveMode.LoadingVars && !adulthoodIdentifier.NullOrEmpty())
            {
                if (!BackstoryDatabase.TryGetWithIdentifier(adulthoodIdentifier, out backStoryAdult, true))
                {
                    //removed boolean as it appeared to be obsolete
                    Log.Error("Couldn't load adult backstory with identifier " + adulthoodIdentifier + ". Giving random.");
                    backStoryAdult = BackstoryDatabase.RandomBackstory(BackstorySlot.Adulthood);
                }
            }
            
            Scribe_Collections.Look(ref skills, "skills", LookMode.Deep);
            Scribe_Values.Look(ref isAnimal, "isAnimal");
            Scribe_Deep.Look(ref trainingLearned, "trainingLearned");
            Scribe_Deep.Look(ref trainingSteps, "trainingSteps");
            Scribe_Collections.Look(ref hediffInfos, "hediffInfos", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.LoadingVars && hediffInfos != null)
            {
                //remove any hediffs where the def is missing. Most commonly occurs when a mod is removed from a save.
                int removed = hediffInfos.RemoveAll(h => h.def == null);
                if (removed > 0)
                {
                    QEEMod.TryLog("Removed " + removed + " null hediffs from hediffInfo list for " + sourceName + "'s brain template ");
                }
            }
        }

        public override string LabelNoCount
        {
            get
            {
                if(GetComp<CustomNameComp>() is CustomNameComp nameComp && nameComp.customName.NullOrEmpty())
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

        public string CustomDescriptionString(string baseDescription)
        {
            StringBuilder builder = new StringBuilder(baseDescription);

            builder.AppendLine();
            builder.AppendLine();
            if (sourceName != null)
            {
                builder.AppendLine("QE_GenomeSequencerDescription_Name".Translate() + ": " + sourceName);
            }
            if (kindDef?.race != null)
            {
                builder.AppendLine("QE_GenomeSequencerDescription_Race".Translate() + ": " + kindDef.race.LabelCap);
            }
            if (backStoryChild != null)
                builder.AppendLine("QE_BrainScanDescription_BackshortChild".Translate() + ": " + backStoryChild.title.CapitalizeFirst());
            if (backStoryAdult != null)
                builder.AppendLine("QE_BrainScanDescription_BackshortAdult".Translate() + ": " + backStoryAdult.title.CapitalizeFirst());
            //ideo
            if(scannedIdeology!=null)
            { builder.AppendLine("Scanned Ideology" + ": " + scannedIdeology); }
            //Skills
            if (!isAnimal && skills.Count > 0)
            {
                builder.AppendLine("QE_BrainScanDescription_Skills".Translate());
                foreach (ComparableSkillRecord skill in skills.OrderBy(skillRecord => skillRecord.def.index))
                {
                    builder.AppendLine(skill.ToString());
                }
            }

            if (isAnimal)
            {
                builder.AppendLine("QE_BrainScanDescription_Training".Translate());
                foreach (var training in trainingSteps.OrderBy(trainingPair => trainingPair.Key.index))
                {
                    builder.AppendLine("    " + training.Key.LabelCap + ": " + training.Value);
                }
            }

            //Hediffs
            HediffInfo.GenerateDescForHediffList(ref builder, hediffInfos);

            return builder.ToString().TrimEndNewlines();
        }

        //this changes the text displayed in the bottom-left info panel when you select the item
        public override string GetInspectString()
        {
            StringBuilder builder = new StringBuilder(base.GetInspectString());

            if (kindDef != null)
            {
                builder.AppendLine("QE_GenomeSequencerDescription_Race".Translate() + ": " + kindDef.race.LabelCap);
            }

            if (backStoryChild != null)
            {
                builder.AppendLine("QE_BrainScanDescription_BackshortChild".Translate() + ": " + backStoryChild.title.CapitalizeFirst());
            }

            if (backStoryAdult != null)
            {
                builder.AppendLine("QE_BrainScanDescription_BackshortAdult".Translate() + ": " + backStoryAdult.title.CapitalizeFirst());
            }

            if (hediffInfos != null)
            {
                if (hediffInfos.Count > 0)
                {
                    builder.AppendLine("QE_GenomeSequencerDescription_Hediffs".Translate() + ": " + hediffInfos.Count);
                }
            }

            return builder.ToString().TrimEndNewlines();
        }

        public override Thing SplitOff(int count)
        {
            Thing splitThing = base.SplitOff(count);

            if(splitThing != this && splitThing is BrainScanTemplate brainScan)
            {
                //Shared
                brainScan.sourceName = sourceName;
                brainScan.kindDef = kindDef;

                //Humanoid
                brainScan.backStoryChild = backStoryChild;
                brainScan.backStoryAdult = backStoryAdult;
                foreach (ComparableSkillRecord skill in skills)
                {
                    brainScan.skills.Add(new ComparableSkillRecord()
                    {
                        def = skill.def,
                        level = skill.level,
                        passion = skill.passion
                    });
                }

                if (hediffInfos != null)
                {
                    foreach (HediffInfo h in hediffInfos)
                    {
                        brainScan.hediffInfos.Add(h);
                    }
                }

                //Animal
                foreach (var item in trainingLearned)
                {
                    brainScan.trainingLearned[item.Key] = item.Value;
                }
                foreach (var item in trainingSteps)
                {
                    brainScan.trainingSteps[item.Key] = item.Value;
                }
            }

            return splitThing;
        }

        /// <summary>
        /// Determines whether a list of ComparableSkillRecords is equivalent to another.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool SkillsEqual(List<ComparableSkillRecord> other)
        {
            if (skills.Count != other.Count)
            {
                return false;
            }

            //UNCOMMENT BELOW FOR ACTIVE DEBUGGING - too spammy even when debugLogging is true
            //bool seqEqual = false;
            //seqEqual = skills.SequenceEqual(other);
            //QEEMod.TryLog("Skills equivalent: " + seqEqual.ToString().ToUpper());
            //return seqEqual;

            return skills.SequenceEqual(other);
        }

        public override bool CanStackWith(Thing other)
        {

            if (other is BrainScanTemplate brainScan &&
                backStoryChild == brainScan.backStoryChild &&
                backStoryAdult == brainScan.backStoryAdult &&
                DefMapsEqual(trainingLearned, brainScan.trainingLearned) &&
                DefMapsEqual(trainingSteps, brainScan.trainingSteps)
                && SkillsEqual(brainScan.skills)
                && sourceName == brainScan.sourceName &&
                (kindDef?.defName != null && brainScan.kindDef?.defName != null && kindDef.defName == brainScan.kindDef.defName
                    || kindDef == null && brainScan.kindDef == null) &&
                (hediffInfos != null && brainScan.hediffInfos != null &&
                    hediffInfos.OrderBy(h => h.def.LabelCap).SequenceEqual(brainScan.hediffInfos.OrderBy(h => h.def.LabelCap))
                    || hediffInfos == null && brainScan.hediffInfos == null))
            {
                return base.CanStackWith(other);
            }

            return false;
        }

        public bool DefMapsEqual<T>(DefMap<TrainableDef, T> mapA, DefMap<TrainableDef,T> mapB) where T: new()
        {
            if(mapA.Count != mapB.Count)
            {
                return false;
            }

            foreach(var pair in mapA)
            {
                var validPairs = mapB.Where(pairB => pairB.Key == pair.Key);
                if(validPairs != null && validPairs.Count() > 0)
                {
                    var pairB = validPairs.First();
                    if(!pair.Value.Equals(pairB.Value))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (var option in base.GetFloatMenuOptions(selPawn))
            {
                yield return option;
            }

            //Start targeter
            yield return new FloatMenuOption("QE_BrainScanningApplyTemplate".Translate(),
                delegate()
                {
                    string failReason = "";
                    TargetingParameters targetParams = 
                    new TargetingParameters()
                    {
                        canTargetPawns = true,

                        //initial target is valid as long as it's a pawn, it's not the one selected
                        //other checks are less obvious to the player, so inform them with a message in IsValidBrainTemplatingTarget()
                        validator = (target) => target.HasThing && target.Thing is Pawn pawn && pawn != selPawn
                    };

                    //do all validation here instead of the validator predicate above, because here we 
                    //can write messages to the player if they select an invalid pawn, telling them *why*
                    //it's the wrong target
                    Find.Targeter.BeginTargeting(targetParams, 
                        delegate(LocalTargetInfo target)
                        {
                            Pawn targetPawn = target.Thing as Pawn;
                            if(targetPawn != null)
                            {
                                //begin validation
                                if (targetPawn.IsValidBrainTemplatingTarget(ref failReason, this))
                                {

                                    //valid target established, time to find a bed.
                                    //Healthy pawns will get up from medical beds immediately, so skip med beds in search
                                    Building_Bed validBed = targetPawn.FindAvailMedicalBed(selPawn);

                                    string whyFailed = "";
                                    if (validBed == null)
                                    {
                                        if (targetPawn.RaceProps.Animal)
                                        {
                                            whyFailed = "No animal beds are available";
                                        }
                                        else
                                        {
                                            whyFailed = "No medical beds are available";
                                        }
                                    }
                                    else if(!selPawn.CanReserveAndReach(targetPawn, PathEndMode.OnCell, Danger.Deadly))
                                    {
                                        whyFailed = selPawn.LabelShort + " can't reach/reserve " + targetPawn.LabelShort;
                                    }
                                    else if(!selPawn.CanReserveAndReach(this, PathEndMode.OnCell, Danger.Deadly))
                                    {
                                        whyFailed = selPawn.LabelShort + " can't reach/reserve the brain template";
                                    }
                                    //check if bed can be reserved, if patient is not already there
                                    else if(targetPawn.CurrentBed() != validBed && 
                                        !selPawn.CanReserveAndReach(validBed, PathEndMode.OnCell, Danger.Deadly))
                                    {
                                        whyFailed = selPawn.LabelShort + " can't reach/reserve the " + validBed.def.defName;
                                    }

                                    if (!String.IsNullOrEmpty(whyFailed))
                                    {
                                        Messages.Message(whyFailed, MessageTypeDefOf.RejectInput, false);
                                        SoundDefOf.ClickReject.PlayOneShot(SoundInfo.OnCamera());
                                    }
                                    else
                                    {
                                        selPawn.jobs.TryTakeOrderedJob(new Job(QEJobDefOf.QE_ApplyBrainScanTemplate, targetPawn, this, validBed)
                                        {
                                            count = 1
                                        });
                                    }

                                }
                                else
                                {
                                    Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                                    SoundDefOf.ClickReject.PlayOneShot(SoundInfo.OnCamera());
                                }
                            }
                        },
                        caster: selPawn);
                });
        }
    }
}
