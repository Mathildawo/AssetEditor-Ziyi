using System;
using System.Linq;
using System.Windows;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.Core.Common.ReferenceModel;
using Editors.Shared.Core.Services;
using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.GameFormats.Animation;
using Shared.Ui.Common;

namespace AnimationEditor.CampaignAnimationCreator
{
    // Correction : ajout du paramètre requis 'IEditorHostParameters inputParams' pour le constructeur de base.
    public partial class CampaignAnimationCreatorViewModel : EditorHostBase
    {
        private AnimationToolInput _animationToolInput;
        private SceneObject _selectedUnit;
        private AnimationClip _selectedAnimationClip;

        private readonly SceneObjectEditor _assetViewModelEditor;
        private readonly SceneObjectViewModelBuilder _referenceModelSelectionViewModelBuilder;
        private readonly IFileSaveService _packFileSaveService;

        public FilterCollection<SkeletonBoneNode> ModelBoneList { get; set; } = new FilterCollection<SkeletonBoneNode>(null);
        public string EditorName => "Campaign Animation Creator";

        public override Type EditorViewModelType => typeof(EditorView);

        public CampaignAnimationCreatorViewModel(
            IEditorHostParameters inputParams,
            SceneObjectEditor assetViewModelEditor,
            SceneObjectViewModelBuilder referenceModelSelectionViewModelBuilder,
            IFileSaveService packFileSaveService,
            AnimationToolInput animationToolInput)
            : base(inputParams)
        {
            _assetViewModelEditor = assetViewModelEditor;
            _referenceModelSelectionViewModelBuilder = referenceModelSelectionViewModelBuilder;
            _packFileSaveService = packFileSaveService;
            _animationToolInput = animationToolInput;
            Initialize();
        }

        public void SetDebugInputParameters(AnimationToolInput debugDataToLoad)
        {
            
        }

        public void Initialize()
        {
            var item = _referenceModelSelectionViewModelBuilder.CreateAsset("model", true, "model", Color.Black, _animationToolInput);
            Create(item.Data);
            SceneObjects.Add(item);
        }

        void Create(SceneObject rider)
        {
            _selectedUnit = rider;
            _selectedUnit.SkeletonChanged += SkeletonChanged;
            _selectedUnit.AnimationChanged += AnimationChanged;

            SkeletonChanged(_selectedUnit.Skeleton);
            AnimationChanged(_selectedUnit.AnimationClip);
        }

        public void SaveAnimation()
        {
            var animFile = _selectedUnit.AnimationClip.ConvertToFileFormat(_selectedUnit.Skeleton);
            var bytes = AnimationFile.ConvertToBytes(animFile);
            _packFileSaveService.SaveAs(".anim", bytes);
        }

        public void Convert()
        {
           if (_selectedAnimationClip == null)
           {
               MessageBox.Show("No animation selected");
               return;
           }
           
           if (ModelBoneList.SelectedItem == null)
           {
               MessageBox.Show("No root bone selected");
               return;
           }
           
           var newAnimation = _selectedAnimationClip.Clone();
           for (var frameIndex = 0; frameIndex < newAnimation.DynamicFrames.Count; frameIndex++)
           {
               var frame = newAnimation.DynamicFrames[frameIndex];
               frame.Position[ModelBoneList.SelectedItem.BoneIndex] = Vector3.Zero;
               frame.Rotation[ModelBoneList.SelectedItem.BoneIndex] = Quaternion.Identity;
           }
           
           _selectedUnit.AnimationChanged -= AnimationChanged;
           _assetViewModelEditor.SetAnimationClip(_selectedUnit, newAnimation, "Generated animation");
           _selectedUnit.AnimationChanged += AnimationChanged;
        }

        private void AnimationChanged(AnimationClip newValue)
        {
            _selectedAnimationClip = newValue;
        }

        private void SkeletonChanged(GameSkeleton newValue)
        {
            if (newValue == null)
            {
                ModelBoneList.UpdatePossibleValues(null);
            }
            else
            {
                ModelBoneList.UpdatePossibleValues(SkeletonBoneNodeHelper.CreateFlatSkeletonList(newValue));
                ModelBoneList.SelectedItem = ModelBoneList.PossibleValues.FirstOrDefault(x => x.BoneName.ToLower() == "animroot");
            }
        }
    }
}
