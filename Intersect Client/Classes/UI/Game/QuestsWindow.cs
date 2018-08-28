﻿using System;
using System.Collections.Generic;
using Intersect.GameObjects;
using Intersect.Client.Classes.Localization;
using Intersect.Enums;
using IntersectClientExtras.File_Management;
using IntersectClientExtras.GenericClasses;
using IntersectClientExtras.Gwen;
using IntersectClientExtras.Gwen.Control;
using IntersectClientExtras.Gwen.Control.EventArguments;
using Intersect_Client.Classes.General;
using Intersect_Client.Classes.Networking;
using Intersect.Client.Classes.Core;

namespace Intersect_Client.Classes.UI.Game
{
    public class QuestsWindow
    {
        private Button mBackButton;
        private ScrollControl mQuestDescArea;
        private RichLabel mQuestDescLabel;
        private Label mQuestDescTemplateLabel;
        private ListBox mQuestList;

        private Label mQuestStatus;

        //Controls
        private WindowControl mQuestsWindow;

        private Label mQuestTitle;
        private Button mQuitButton;
        private QuestBase mSelectedQuest;

        //Init
        public QuestsWindow(Canvas gameCanvas)
        {
            mQuestsWindow = new WindowControl(gameCanvas, Strings.QuestLog.title, false, "QuestsWindow");
            mQuestsWindow.DisableResizing();

            mQuestList = new ListBox(mQuestsWindow, "QuestList");
            mQuestList.EnableScroll(false, true);

            mQuestTitle = new Label(mQuestsWindow, "QuestTitle");
            mQuestTitle.SetText("");

            mQuestStatus = new Label(mQuestsWindow, "QuestStatus");
            mQuestStatus.SetText("");

            mQuestDescArea = new ScrollControl(mQuestsWindow, "QuestDescription");

            mQuestDescTemplateLabel = new Label(mQuestsWindow, "QuestDescriptionTemplate");

            mQuestDescLabel = new RichLabel(mQuestDescArea);

            mBackButton = new Button(mQuestsWindow, "BackButton");
            mBackButton.Clicked += _backButton_Clicked;

            mQuitButton = new Button(mQuestsWindow, "AbandonQuestButton");
            mQuitButton.SetText(Strings.QuestLog.abandon);
            mQuitButton.Clicked += _quitButton_Clicked;

            mQuestsWindow.LoadJsonUi(GameContentManager.UI.InGame, GameGraphics.Renderer.GetResolutionString());
        }

        private void _quitButton_Clicked(Base sender, ClickedEventArgs arguments)
        {
            if (mSelectedQuest != null)
            {
                new InputBox(Strings.QuestLog.abandontitle.ToString(mSelectedQuest.Name),
                    Strings.QuestLog.abandonprompt.ToString(mSelectedQuest.Name), true, InputBox.InputType.YesNo,
                    AbandonQuest, null,
                    mSelectedQuest.Id);
            }
        }

        void AbandonQuest(object sender, EventArgs e)
        {
            PacketSender.SendCancelQuest((Guid)((InputBox) sender).UserData);
        }

        private void _backButton_Clicked(Base sender, ClickedEventArgs arguments)
        {
            mSelectedQuest = null;
            UpdateSelectedQuest();
        }

        //Methods
        public void Update(bool shouldUpdateList)
        {
            if (shouldUpdateList)
            {
                UpdateQuestList();
                UpdateSelectedQuest();
            }
            if (mQuestsWindow.IsHidden)
            {
                return;
            }

            if (mSelectedQuest != null)
            {
                if (Globals.Me.QuestProgress.ContainsKey(mSelectedQuest.Id))
                {
                    if (Globals.Me.QuestProgress[mSelectedQuest.Id].Completed == 1 &&
                        Globals.Me.QuestProgress[mSelectedQuest.Id].TaskId == Guid.Empty)
                    {
                        //Completed
                        if (mSelectedQuest.LogAfterComplete == 0)
                        {
                            mSelectedQuest = null;
                            UpdateSelectedQuest();
                        }
                        return;
                    }
                    else
                    {
                        if (Globals.Me.QuestProgress[mSelectedQuest.Id].TaskId == Guid.Empty)
                        {
                            //Not Started
                            if (mSelectedQuest.LogBeforeOffer == 0)
                            {
                                mSelectedQuest = null;
                                UpdateSelectedQuest();
                            }
                        }
                        return;
                    }
                }
                if (mSelectedQuest.LogBeforeOffer == 0)
                {
                    mSelectedQuest = null;
                    UpdateSelectedQuest();
                }
            }
        }

        private void UpdateQuestList()
        {
            mQuestList.RemoveAllRows();
            if (Globals.Me != null)
            {
                var quests = QuestBase.Lookup.Values;
                foreach (QuestBase quest in quests)
                {
                    if (quest != null)
                    {
                        if (Globals.Me.QuestProgress.ContainsKey(quest.Id))
                        {
                            if (Globals.Me.QuestProgress[quest.Id].TaskId != Guid.Empty)
                            {
                                AddQuestToList(quest.Name, Color.Yellow, quest.Id);
                            }
                            else
                            {
                                if (Globals.Me.QuestProgress[quest.Id].Completed == 1)
                                {
                                    if (quest.LogAfterComplete == 1)
                                    {
                                        AddQuestToList(quest.Name, Color.Green, quest.Id);
                                    }
                                }
                                else
                                {
                                    if (quest.LogBeforeOffer == 1)
                                    {
                                        AddQuestToList(quest.Name, Color.Red, quest.Id);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (quest.LogBeforeOffer == 1)
                            {
                                AddQuestToList(quest.Name, Color.Red, quest.Id);
                            }
                        }
                    }
                }
            }
        }

        private void AddQuestToList(string name, Color clr, Guid questId)
        {
            var item = mQuestList.AddRow(name);
            item.UserData = questId;
            item.Clicked += QuestListItem_Clicked;
            item.Selected += Item_Selected;
            item.SetTextColor(clr);
        }

        private void Item_Selected(Base sender, ItemSelectedEventArgs arguments)
        {
            mQuestList.UnselectAll();
        }

        private void QuestListItem_Clicked(Base sender, ClickedEventArgs arguments)
        {
            var questNum = (Guid) ((ListBoxRow) sender).UserData;
            var quest = QuestBase.Get(questNum);
            if (quest != null)
            {
                mSelectedQuest = quest;
                UpdateSelectedQuest();
            }
            mQuestList.UnselectAll();
        }

        private void UpdateSelectedQuest()
        {
            if (mSelectedQuest == null)
            {
                mQuestList.Show();
                mQuestTitle.Hide();
                mQuestDescArea.Hide();
                mQuestStatus.Hide();
                mBackButton.Hide();
                mQuitButton.Hide();
            }
            else
            {
                mQuestDescLabel.ClearText();
                ListBoxRow rw;
                string[] myText = null;
                List<string> taskString = new List<string>();
                if (Globals.Me.QuestProgress.ContainsKey(mSelectedQuest.Id))
                {
                    if (Globals.Me.QuestProgress[mSelectedQuest.Id].TaskId != Guid.Empty)
                    {
                        //In Progress
                        mQuestStatus.SetText(Strings.QuestLog.inprogress);
                        mQuestStatus.SetTextColor(Color.Yellow, Label.ControlState.Normal);
                        if (mSelectedQuest.InProgressDesc.Length > 0)
                        {
                            mQuestDescLabel.AddText(mSelectedQuest.InProgressDesc, Color.White, Alignments.Left,
                                mQuestDescTemplateLabel.Font);
                            mQuestDescLabel.AddLineBreak();
                            mQuestDescLabel.AddLineBreak();
                        }
                        mQuestDescLabel.AddText(Strings.QuestLog.currenttask, Color.White, Alignments.Left,
                            mQuestDescTemplateLabel.Font);
                        mQuestDescLabel.AddLineBreak();
                        for (int i = 0; i < mSelectedQuest.Tasks.Count; i++)
                        {
                            if (mSelectedQuest.Tasks[i].Id == Globals.Me.QuestProgress[mSelectedQuest.Id].TaskId)
                            {
                                if (mSelectedQuest.Tasks[i].Description.Length > 0)
                                {
                                    mQuestDescLabel.AddText(mSelectedQuest.Tasks[i].Description, Color.White, Alignments.Left,
                                        mQuestDescTemplateLabel.Font);
                                    mQuestDescLabel.AddLineBreak();
                                    mQuestDescLabel.AddLineBreak();
                                }
                                if (mSelectedQuest.Tasks[i].Objective == QuestObjective.GatherItems) //Gather Items
                                {
                                    mQuestDescLabel.AddText(Strings.QuestLog.taskitem.ToString(
                                            Globals.Me.QuestProgress[mSelectedQuest.Id].TaskProgress,
                                            mSelectedQuest.Tasks[i].Quantity,
                                            ItemBase.GetName(mSelectedQuest.Tasks[i].TargetId)),
                                        Color.White, Alignments.Left, mQuestDescTemplateLabel.Font);
                                }
                                else if (mSelectedQuest.Tasks[i].Objective == QuestObjective.KillNpcs) //Kill Npcs
                                {
                                    mQuestDescLabel.AddText(Strings.QuestLog.tasknpc.ToString(
                                            Globals.Me.QuestProgress[mSelectedQuest.Id].TaskProgress,
                                            mSelectedQuest.Tasks[i].Quantity,
                                            NpcBase.GetName(mSelectedQuest.Tasks[i].TargetId)),
                                        Color.White, Alignments.Left, mQuestDescTemplateLabel.Font);
                                }
                            }
                        }
                        if (mSelectedQuest.Quitable == 1)
                        {
                            mQuitButton.Show();
                        }
                    }
                    else
                    {
                        if (Globals.Me.QuestProgress[mSelectedQuest.Id].Completed == 1)
                        {
                            //Completed
                            if (mSelectedQuest.LogAfterComplete == 1)
                            {
                                mQuestStatus.SetText(Strings.QuestLog.completed);
                                mQuestStatus.SetTextColor(Color.Green, Label.ControlState.Normal);
                                mQuestDescLabel.AddText(mSelectedQuest.EndDesc, Color.White, Alignments.Left,
                                    mQuestDescTemplateLabel.Font);
                            }
                        }
                        else
                        {
                            //Not Started
                            if (mSelectedQuest.LogBeforeOffer == 1)
                            {
                                mQuestStatus.SetText(Strings.QuestLog.notstarted);
                                mQuestStatus.SetTextColor(Color.Red, Label.ControlState.Normal);
                                mQuestDescLabel.AddText(mSelectedQuest.BeforeDesc, Color.White, Alignments.Left,
                                    mQuestDescTemplateLabel.Font);
                                mQuitButton?.Hide();
                            }
                        }
                    }
                }
                else
                {
                    //Not Started
                    if (mSelectedQuest.LogBeforeOffer == 1)
                    {
                        mQuestStatus.SetText(Strings.QuestLog.notstarted);
                        mQuestStatus.SetTextColor(Color.Red, Label.ControlState.Normal);
                        mQuestDescLabel.AddText(mSelectedQuest.BeforeDesc, Color.White, Alignments.Left,
                            mQuestDescTemplateLabel.Font);
                    }
                }
                mQuestList.Hide();
                mQuestTitle.IsHidden = false;
                mQuestTitle.Text = mSelectedQuest.Name;
                mQuestDescArea.IsHidden = false;
                mQuestDescLabel.Width = mQuestDescArea.Width - mQuestDescArea.GetVerticalScrollBar().Width;
                mQuestDescLabel.SizeToChildren(false, true);
                mQuestStatus.Show();
                mBackButton.Show();
            }
        }

        public void Show()
        {
            mQuestsWindow.IsHidden = false;
        }

        public bool IsVisible()
        {
            return !mQuestsWindow.IsHidden;
        }

        public void Hide()
        {
            mQuestsWindow.IsHidden = true;
            mSelectedQuest = null;
        }
    }
}