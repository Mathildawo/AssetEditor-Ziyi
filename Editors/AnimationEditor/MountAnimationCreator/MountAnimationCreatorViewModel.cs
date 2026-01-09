using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AnimationEditor.AnimationKeyframeEditor;
using AnimationEditor.MountAnimationCreator.Services;
using AnimationEditor.MountAnimationCreator.ViewModels;
using Editors.AnimationVisualEditors.MountAnimationCreator.Services;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Common.AnimationPlayer;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.Core.Common.ReferenceModel;
using GameWorld.Core.Animation;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Shared.Core.ByteParsing;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.Settings;
using Shared.GameFormats.AnimationPack;
using Shared.GameFormats.AnimationPack.AnimPackFileTypes;
using Shared.Ui.Common;
using Shared.Ui.Events.UiCommands;
using static AnimationEditor.MountAnimationCreator.AnimationSetEntry;
using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.Forms.MessageBox;

namespace AnimationEditor.MountAnimationCreator
{
    public partial class MountAnimationCreatorViewModel : EditorHostBase //NotifyPropertyChangedImpl, IHostedEditor<MountAnimationCreatorViewModel>
    {
        public override Type EditorViewModelType => typeof(EditorView);
        private readonly SceneObjectViewModelBuilder _sceneObjectViewModelBuilder;
        private readonly SceneObjectEditor _sceneObjectBuilder;
        private readonly IFileSaveService _fileSaveService;
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly AnimationPlayerViewModel _animationPlayerViewModel;
        private readonly IPackFileService _pfs;
        private readonly SelectionManager _selectionManager;
        private readonly ISkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;

        SceneObject _mount;
        SceneObject _rider;
        SceneObject _newAnimation;
  
        List<int> _mountVertexes = new();
        Rmv2MeshNode _mountVertexOwner;

        AnimationToolInput _inputRiderData;
        AnimationToolInput _inputMountData;

        public AnimationSettingsViewModel AnimationSettings { get; set; } = new AnimationSettingsViewModel();
        public MountLinkViewModel MountLinkController { get; set; }
        public string EditorName => "Mount Animation Creator";

        public FilterCollection<SkeletonBoneNode> SelectedRiderBone { get; set; }

        public NotifyAttr<bool> CanPreview { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> CanBatchProcess { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> CanSave { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> CanAddToFragment { get; set; } = new NotifyAttr<bool>(false);

        public NotifyAttr<bool> DisplayGeneratedSkeleton { get; set; }
        public NotifyAttr<bool> DisplayGeneratedMesh { get; set; }

        public NotifyAttr<string> SelectedVertexesText { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<string> SavePrefixText { get; set; } = new NotifyAttr<string>("new_");
        public ObservableCollection<uint> AnimationOutputFormats { get; set; } = new ObservableCollection<uint>() { 5, 6, 7 };
        public NotifyAttr<uint> SelectedAnimationOutputFormat { get; set; } = new NotifyAttr<uint>(7);
        public NotifyAttr<bool> EnsureUniqeFileName { get; set; } = new NotifyAttr<bool>(true);

        public FilterCollection<IAnimationBinGenericFormat> ActiveOutputFragment { get; set; }

        public FilterCollection<FragmentStatusSlotItem> ActiveFragmentSlot { get; set; }

        public static class AnimationRetargetIds
        {
            public static string Rider => "Rider";
            public static string Mount => "Mount";
            public static string Generated => "Generated";
        }

        public MountAnimationCreatorViewModel(IPackFileService pfs,
            ISkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, 
            SelectionManager selectionManager,
            SceneObjectViewModelBuilder sceneObjectViewModelBuilder,
            AnimationPlayerViewModel animationPlayerViewModel,
            SceneObjectEditor sceneObjectBuilder,
            IEditorHostParameters editorHostParameters,
            IFileSaveService fileSaveService,
            IUiCommandFactory uiCommandFactory) : base(editorHostParameters)
        {
            DisplayName = EditorName; // "Mount Animation Creator";
            _sceneObjectViewModelBuilder = sceneObjectViewModelBuilder;
            _animationPlayerViewModel = animationPlayerViewModel;
            _sceneObjectBuilder = sceneObjectBuilder;
            _fileSaveService = fileSaveService;
            _uiCommandFactory = uiCommandFactory;
            _pfs = pfs;

            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _selectionManager = selectionManager;

            DisplayGeneratedSkeleton = new NotifyAttr<bool>(true, (value) => _newAnimation.ShowSkeleton.Value = value);
            DisplayGeneratedMesh = new NotifyAttr<bool>(true, (value) => { if (_newAnimation.MainNode != null) _newAnimation.ShowMesh.Value = value; });

            SelectedRiderBone = new FilterCollection<SkeletonBoneNode>(null, (x) => UpdateCanSaveAndPreviewStates());


            ActiveOutputFragment = new FilterCollection<IAnimationBinGenericFormat>(null, OutputAnimationSetSelected);
            ActiveOutputFragment.SearchFilter = (value, rx) => { return rx.Match(value.FullPath).Success; };

            ActiveFragmentSlot = new FilterCollection<FragmentStatusSlotItem>(null, (x) => UpdateCanSaveAndPreviewStates());
            ActiveFragmentSlot.SearchFilter = (value, rx) => { return rx.Match(value.Entry.Value.Slot.Value).Success; };

            AnimationSettings.SettingsChanged += () => TryReGenerateAnimation(null);

            Initialize();
        }

        public void SetDebugInputParameters(AnimationToolInput rider, AnimationToolInput mount)
        {
            _inputRiderData = rider;
            _inputMountData = mount;
        }
        void Initialize() //EditorHost<MountAnimationCreatorViewModel> owner)
        {
           var riderItem = _sceneObjectViewModelBuilder.CreateAsset(AnimationRetargetIds.Rider, true, "Rider", Color.Black, _inputRiderData);
           var mountItem = _sceneObjectViewModelBuilder.CreateAsset(AnimationRetargetIds.Mount, true, "Mount", Color.Black, _inputMountData);
           mountItem.Data.IsSelectable = true;
           
           var propAsset = _sceneObjectBuilder.CreateAsset(AnimationRetargetIds.Generated, "New Anim", Color.Red);
           _animationPlayerViewModel.RegisterAsset(propAsset);
           
           Create(riderItem.Data, mountItem.Data, propAsset);
           SceneObjects.Add(riderItem);
           SceneObjects.Add(mountItem);
        }

        internal void Create(SceneObject rider, SceneObject mount, SceneObject newAnimation)
        {
            _newAnimation = newAnimation;
            _mount = mount;
            _rider = rider;

            _mount.SkeletonChanged += MountSkeletonChanged;
            _mount.AnimationChanged += TryReGenerateAnimation;
            _rider.SkeletonChanged += RiderSkeletonChanges;
            _rider.AnimationChanged += TryReGenerateAnimation;

            MountLinkController = new MountLinkViewModel(_sceneObjectBuilder, _pfs, _skeletonAnimationLookUpHelper, rider, mount, UpdateCanSaveAndPreviewStates);

            MountSkeletonChanged(_mount.Skeleton);
            RiderSkeletonChanges(_rider.Skeleton);
        }

        private void TryReGenerateAnimation(AnimationClip newValue = null)
        {
             UpdateCanSaveAndPreviewStates();
             if (CanPreview.Value)
                 CreateMountAnimationAction();
             else
             {
                 if (_newAnimation != null)
                     _sceneObjectBuilder.SetAnimation(_newAnimation, null);
             }
        }

        private void MountSkeletonChanged(GameSkeleton newValue)
        {
            UpdateCanSaveAndPreviewStates();
            MountLinkController.ReloadFragments(false, true);
        }

        List<IAnimationBinGenericFormat> LoadFragmentsForSkeleton(string skeletonName, bool onlyPacksThatCanBeSaved = false)
        {
            var outputFragments = new List<IAnimationBinGenericFormat>();
            var animPacks = PackFileServiceUtility.GetAllAnimPacks(_pfs);
            foreach (var animPack in animPacks)
            {
                if (onlyPacksThatCanBeSaved == true)
                {
                    if (_pfs.GetPackFileContainer(animPack).IsCaPackFile)
                        continue;
                }

                var animPackFile = AnimationPackSerializer.Load(animPack, _pfs);
                var fragments = animPackFile.GetGenericAnimationSets(skeletonName);
                foreach (var fragment in fragments)
                    outputFragments.Add(fragment);
            }
            return outputFragments;
        }

        private void RiderSkeletonChanges(GameSkeleton newValue)
        {
            if (newValue == null)
            {
                ActiveOutputFragment.UpdatePossibleValues(null);
                SelectedRiderBone.UpdatePossibleValues(null);
            }
            else
            {
                ActiveOutputFragment.UpdatePossibleValues(LoadFragmentsForSkeleton(newValue.SkeletonName, false));
                SelectedRiderBone.UpdatePossibleValues(SkeletonBoneNodeHelper.CreateFlatSkeletonList(newValue));  
            }

            // Try setting using root bone
            SelectedRiderBone.SelectedItem = SelectedRiderBone.PossibleValues.FirstOrDefault(x => string.Equals("root", x.BoneName, StringComparison.OrdinalIgnoreCase));
            AnimationSettings.IsRootNodeAnimation = SelectedRiderBone.SelectedItem != null;

            // Try setting using hip bone
            if (AnimationSettings.IsRootNodeAnimation == false)
                SelectedRiderBone.SelectedItem = SelectedRiderBone.PossibleValues.FirstOrDefault(x => string.Equals("bn_hips", x.BoneName, StringComparison.OrdinalIgnoreCase));

            MountLinkController.ReloadFragments(true, false);
            UpdateCanSaveAndPreviewStates();
        }

        void OutputAnimationSetSelected(IAnimationBinGenericFormat animationSet)
        {
            if (animationSet == null)
                ActiveFragmentSlot.UpdatePossibleValues(null);
            else
                ActiveFragmentSlot.UpdatePossibleValues(animationSet.Entries.Select(x => new FragmentStatusSlotItem(x)));
            UpdateCanSaveAndPreviewStates();
        }

        void UpdateCanSaveAndPreviewStates()
        {
            var mountConnectionOk = SelectedRiderBone.SelectedItem != null && _mountVertexes.Count != 0;
            var mountOK = _mount != null && _mount.AnimationClip != null && _mount.Skeleton != null;
            var riderOK = _rider != null && _rider.AnimationClip != null && _rider.Skeleton != null;
            CanPreview.Value = mountConnectionOk && _mountVertexes.Count != 0 && mountOK && riderOK;
            CanBatchProcess.Value = MountLinkController?.AnimationSetForMount?.SelectedItem != null && MountLinkController?.AnimationSetForRider?.SelectedItem != null && mountConnectionOk;
            CanAddToFragment.Value = ActiveOutputFragment?.SelectedItem != null && ActiveFragmentSlot?.SelectedItem != null;
            CanSave.Value = mountConnectionOk && _mountVertexes.Count != 0 && mountOK && riderOK;
        }

        public void SetMountVertex()
        {
            var state = _selectionManager.GetState<VertexSelectionState>();
            if (state == null || state.CurrentSelection().Count == 0)
            {
                SelectedVertexesText.Value = "No vertex selected";
                _mountVertexes.Clear();
                _mountVertexOwner = null;
                MessageBox.Show(SelectedVertexesText.Value);
            }
            else
            {
                SelectedVertexesText.Value = $"{state.CurrentSelection().Count} vertexes selected";
                _mountVertexOwner = state.RenderObject as Rmv2MeshNode;
                _mountVertexes = new List<int>(state.CurrentSelection());
            }

            UpdateCanSaveAndPreviewStates();
        }

        public void CreateMountAnimationAction()
        {
           var newRiderAnim = CreateAnimationGenerator().GenerateMountAnimation(_mount.AnimationClip, _rider.AnimationClip);
           
           // Apply
           _sceneObjectBuilder.CopyMeshFromOther(_newAnimation, _rider);
           _sceneObjectBuilder.SetAnimationClip(_newAnimation, newRiderAnim, new AnimationReference("Generated animation", null).ToString());
           _newAnimation.ShowSkeleton.Value = DisplayGeneratedSkeleton.Value;
           _newAnimation.ShowMesh.Value = DisplayGeneratedMesh.Value;
           UpdateCanSaveAndPreviewStates();
        }

        MountAnimationGeneratorService CreateAnimationGenerator()
        {
            return new MountAnimationGeneratorService(AnimationSettings, _mountVertexOwner, _mountVertexes.First(), SelectedRiderBone.SelectedItem.BoneIndex, _rider, _mount);
        }

        public void AddAnimationToFragment()
        {
            // Find stuff in active slot.

            var selectedAnimationSlot = MountLinkController.SelectedRiderTag.SelectedItem;
            if (selectedAnimationSlot == null)
            {
                MessageBox.Show("No animation slot selected");
                return;
            }
            
            AnimationClip newRiderClip = null;
            if (MountAnimationGeneratorService.IsCopyOnlyAnimation(selectedAnimationSlot.SlotName))
                newRiderClip = _rider.AnimationClip;
            else if (_mount.AnimationClip == null || _rider.AnimationClip == null) 
               {
                MessageBox.Show("Need Rider and Mount animation");
                return; 
                }
            else
                newRiderClip = CreateAnimationGenerator().GenerateMountAnimation(_mount.AnimationClip, _rider.AnimationClip);
            
            var fileResult = MountAnimationGeneratorService.SaveAnimation(_pfs, _rider.AnimationName.Value, SavePrefixText.Value, EnsureUniqeFileName.Value, newRiderClip, _newAnimation.Skeleton, _fileSaveService);
            if (fileResult == null)
                {
                    MessageBox.Show("failed to save Animation");
                    return;
                }
            
            var newAnimSlot = selectedAnimationSlot;
            newAnimSlot.AnimationFile = _pfs.GetFullPath(fileResult);
            newAnimSlot.SlotName = ActiveFragmentSlot.SelectedItem.Entry.Value.Slot.Value;

            var toRemove = ActiveOutputFragment.SelectedItem.Entries.FirstOrDefault(x => x.SlotIndex == ActiveFragmentSlot.SelectedItem.Entry.Value.Slotindex);
            ActiveOutputFragment.SelectedItem.Entries.Remove(toRemove);
            
            ActiveOutputFragment.SelectedItem.Entries.Add(newAnimSlot);
            
            var bytes = AnimationPackSerializer.ConvertToBytes(ActiveOutputFragment.SelectedItem.PackFileReference);
            _fileSaveService.Save("animations\\animation_tables\\" + ActiveOutputFragment.SelectedItem.FullPath, bytes, false);
            
            // Update status for the slot thing 
            var possibleValues = ActiveOutputFragment.SelectedItem.Entries.Select(x => new FragmentStatusSlotItem(x));
            ActiveFragmentSlot.UpdatePossibleValues((IEnumerable<FragmentStatusSlotItem>?)possibleValues);
            MountLinkController.ReloadFragments(true, false);
        }

        public void ViewMountFragmentAction() => ViewAnimationSet(MountLinkController.AnimationSetForMount.SelectedItem);
        public void ViewRiderFragmentAction() => ViewAnimationSet(MountLinkController.AnimationSetForRider.SelectedItem);
        public void ViewOutputFragmentAction() => ViewAnimationSet(ActiveOutputFragment.SelectedItem);

        void ViewAnimationSet(IAnimationBinGenericFormat animationSet)
        {
            if (animationSet != null)
            {
                var animpackFileName = animationSet.PackFileReference.FileName;
                _uiCommandFactory.Create<OpenEditorCommand>().ExecuteAsWindow(animpackFileName, 800, 900);
            }
        }

        public void RefreshViewAction()
        {
            MountLinkController.ReloadFragments();
            ActiveOutputFragment.UpdatePossibleValues(MountLinkController.LoadAnimationSetForSkeleton(_rider.SkeletonName.Value, true));
        }

        public void SaveCurrentAnimationAction()
        {
            var service = new BatchProcessorService(_pfs, _skeletonAnimationLookUpHelper, CreateAnimationGenerator(), new BatchProcessOptions { SavePrefix = SavePrefixText.Value }, _fileSaveService, SelectedAnimationOutputFormat.Value);
            service.SaveSingleAnim(_mount.AnimationClip, _rider.AnimationClip, _rider.AnimationName.Value);
        }

        public void BatchProcessUsingFragmentsAction()
        {
            var mountFrag = MountLinkController.AnimationSetForMount.SelectedItem;
            var riderFrag = MountLinkController.AnimationSetForRider.SelectedItem;

            var newFileName = SavePrefixText.Value + Path.GetFileNameWithoutExtension(riderFrag.FullPath);
            var batchSettings = BatchProcessOptionsWindow.ShowDialog(newFileName, SavePrefixText.Value);
            if (batchSettings != null)
            {
                var service = new BatchProcessorService(_pfs, _skeletonAnimationLookUpHelper, CreateAnimationGenerator(), batchSettings, _fileSaveService, SelectedAnimationOutputFormat.Value);
                service.Process(mountFrag, riderFrag);
                MountLinkController.ReloadFragments(true, false);

                ActiveOutputFragment.UpdatePossibleValues(MountLinkController.LoadAnimationSetForSkeleton(_rider.Skeleton.SkeletonName, true));
            }
        }

        public void CopyAnimation()
        {
            if (_newAnimation.AnimationClip == null)
            {
                MessageBox.Show("new animation not generated!", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _newAnimation.Player.Pause();

            var currentFrame = 0;
            var endFrame = _newAnimation.AnimationClip.DynamicFrames.Count;
            var skeleton = _newAnimation.Skeleton;
            var frames = _newAnimation.AnimationClip;
            var jsonText = JsonConvert.SerializeObject(AnimationCliboardCreator.CreateFrameClipboard(skeleton, frames, currentFrame, endFrame));
            Clipboard.SetText(jsonText);
            MessageBox.Show($"copied frame {currentFrame} up to {endFrame - 1}", "warn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
    public class AnimationSlotType
    {
        public int Id { get; set; }
        public string Value { get; set; }

        public AnimationSlotType(int id, string value)
        {
            Id = id;
            Value = value.ToUpper();
        }

        public AnimationSlotType()
        { }

        public AnimationSlotType Clone()
        {
            return new AnimationSlotType(Id, Value);
        }

        public override string ToString()
        {
            return $"{Value}[{Id}]";
        }
    }

    public class AnimationSetEntry
        {
            int _id { get; set; } = 0;
            int _slot { get; set; } = -1;

            public int Slotindex { get; set; } = -1;
            public AnimationSlotType Slot { get; set; }
            public string AnimationFile { get; set; } = string.Empty;


            public AnimationSetEntry(int index, int slotindex, string file, string slotname) // data, GameTypeEnum preferedGame)
            {
                _id = index;
                _slot = slotindex;

                Slot = new AnimationSlotType(slotindex, slotname);
                AnimationFile = file;
                Slotindex = slotindex;
        }

            public AnimationSetEntry()
            { }
            public class FragmentStatusSlotItem
            {
                public NotifyAttr<bool> IsValid { get; set; } = new NotifyAttr<bool>(false);
                public NotifyAttr<AnimationSetEntry> Entry { get; set; } = new NotifyAttr<AnimationSetEntry>(null);

                public FragmentStatusSlotItem(AnimationBinEntryGenericFormat entry)
                {
                    Entry.Value = new AnimationSetEntry(entry.Index, entry.SlotIndex, entry.AnimationFile, entry.SlotName);
                    IsValid.Value = !string.IsNullOrWhiteSpace(entry.AnimationFile);
                }
            }


    }
}
