using System.IO;
using CommonControls.SelectionListDialog;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.AnimatioReTarget.Editor.BoneHandling;
using Editors.AnimatioReTarget.Editor.Settings;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Common.AnimationPlayer;
using Editors.Shared.Core.Services;
using GameWorld.Core.Animation;
using GameWorld.Core.Services;
using Serilog;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.GameFormats.Animation;
using Shared.Ui.BaseDialogs.SelectionListDialog;
using Shared.Ui.Common;
using Shared.Ui.Editors.BoneMapping;
using Xceed.Wpf.Toolkit.Media.Animation;

namespace Editors.AnimatioReTarget.Editor.Saving
{


    public partial class SaveManager : ObservableObject
    {
        //private readonly ILogger _logger = Logging.Create<SaveWindow>();

        private readonly ISkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        private readonly IPackFileService _pfs;
        private readonly IFileSaveService _saveService;
        private readonly IStandardDialogs _standardDialogs;
        private readonly IAbstractFormFactory<SaveWindow> _settingsWindowFactory;
        protected readonly SceneObjectEditor _sceneObjectEditor;
        protected SceneObjectEditor SceneObjectEditor => _sceneObjectEditor;

        private AnimationPlayerViewModel _player;
        private AnimationGenerationSettings _animationSettings;
        private BoneManager _boneManager;

        [ObservableProperty] SaveSettings _settings;
        

        SceneObject _generated;
        SceneObject _source;
        SceneObject _target;

        public SaveManager(BoneManager boneManager,
            ISkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, 
            IPackFileService pfs,
            SaveSettings saveSettings,
            IFileSaveService saveService,
            IStandardDialogs standardDialogs,
            IAbstractFormFactory<SaveWindow> settingsWindowFactory)
        {
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _pfs = pfs;
            _saveService = saveService;
            _standardDialogs = standardDialogs;
            _settingsWindowFactory = settingsWindowFactory;
            _settings = saveSettings;
            _boneManager = boneManager;
        }

        public void SetSceneNodes(SceneObject source, SceneObject target, SceneObject generated, AnimationPlayerViewModel player, AnimationGenerationSettings animationSettings)
        {
            _generated = generated;
            _source = source;
            _target = target;
            _player = player;
            _animationSettings = animationSettings;
        }

        public void SaveAnimation(bool prompOnConflict = true)
        {
            if (_generated?.AnimationClip == null)
            {
                _standardDialogs.ShowDialogBox("Generated skeleton not set, or animation not created");
                return;
            }

            var animationName = _source.AnimationName.Value;
            var clip = _generated.AnimationClip;
           
            var animFile = clip.ConvertToFileFormat(_generated.Skeleton);

             var orgSkeleton = _source.Skeleton.SkeletonName;
             var newSkeleton = _target.Skeleton.SkeletonName;
             var newPath = animationName.Replace(orgSkeleton, newSkeleton);

            if (Settings.AnimationFormat != 7)
            {
                var skeleton = _skeletonAnimationLookUpHelper.GetSkeletonFileFromName(animFile.Header.SkeletonName);
                animFile.ConvertToVersion(Settings.AnimationFormat, skeleton, _pfs);
            }

            var currentFileName = Path.GetFileName(newPath);
            newPath = newPath.Replace(currentFileName, Settings.SavePrefix + currentFileName);
            newPath = SaveUtility.EnsureEnding(newPath, ".anim");

            _saveService.Save(newPath, AnimationFile.ConvertToBytes(animFile), prompOnConflict);

        }
        [RelayCommand] void ShowSaveSettings()
        {
            var window = _settingsWindowFactory.Create();
            window.Initialize(this);
            window.ShowDialog();
        }

        
        public void OpenBatchProcessDialog()
        {
            if (!CanUpdateAnimation(false))
                return;

            // Find all animations for skeleton
            var copyFromAnims = _skeletonAnimationLookUpHelper.GetAnimationsForSkeleton(_source.Skeleton.SkeletonName);
            
            var items = copyFromAnims.Select(x => new SelectionListViewModel<AnimationReference>.Item()
            {
                IsChecked = new NotifyAttr<bool>(!(x.AnimationFile.Contains("tech", StringComparison.InvariantCultureIgnoreCase) || x.AnimationFile.Contains("skeletons", StringComparison.InvariantCultureIgnoreCase))),
                DisplayName = x.AnimationFile,
                ItemValue = x
            }).ToList();

            var window = SelectionListWindow.ShowDialog("Select animations:", items);
            if (window.Result)
            {
                using (var waitCursor = new WaitCursor())
                {
                    var index = 1;
                    var numItemsToProcess = items.Count(x => x.IsChecked.Value);
                    if (numItemsToProcess > 50)
                    {
                        var confirm = _standardDialogs.ShowYesNoBox("about to process 50 or more items! continue milord?", "ok/Cancel");
                        if (confirm != ShowMessageBoxResult.OK) return;
                    }
                    foreach (var item in items)
                    {
                        if (item.IsChecked.Value)
                        {
                            var file = _pfs.FindFile(item.ItemValue.AnimationFile, item.ItemValue.Container);
                            var animFile = AnimationFile.Create(file.DataSource.ReadDataAsChunk()); 
                            var clip = new AnimationClip(animFile, _source.Skeleton);

                            Console.WriteLine($"Processing animation {index} / {numItemsToProcess} - {item.DisplayName}");

                            var updatedClip = UpdateAnimation(clip, null);
                            _source.AnimationName.Value = item.ItemValue.AnimationFile;
                            _generated.AnimationClip = updatedClip;
                            SaveAnimation( false);
                        }
                        index++;
                    }
                }
            }
        }



        public void UpdateAnimation()
        {
         //if (CanUpdateAnimation(true))
            {
                var newAnimationClip = UpdateAnimation(_source.AnimationClip, _target.AnimationClip);
                _sceneObjectEditor.SetAnimationClip(_generated, newAnimationClip, "Generated animation");
                _player.SelectedMainAnimation = _player.PlayerItems.First(x => x.Asset == _generated);
            }
        }

                AnimationClip UpdateAnimation(AnimationClip animationToCopy, AnimationClip originalAnimation)
                {
                    var service = new AnimationRemapperService(_animationSettings, _boneManager.Bones); //_remappingInformation, Bones);
                    var newClip = service.ReMapAnimation(_source.Skeleton, _target.Skeleton, animationToCopy);
                    return newClip;
                }


        bool CanUpdateAnimation(bool requireAnimation)
        {
        //    if (_remappingInformation == null)
        //    {
        //        _standardDialogs.ShowDialogBox("No mapping created?", "Error");
        //        return false;
        //    }

            if (_target.Skeleton == null || _source.Skeleton == null)
            {
                _standardDialogs.ShowDialogBox("Missing a skeleton?", "Error");
                return false;
            }

            if (_source.AnimationClip == null && requireAnimation)
            {
                _standardDialogs.ShowDialogBox("No animation to copy selected", "Error");
                return false;
            }

            return true;
        }


        public void SaveAnimationAction()
        {
            if (_generated.AnimationClip == null || _generated.Skeleton == null || _source.Skeleton == null)
            {
                _standardDialogs.ShowDialogBox("Can not save, as no animation has been generated. Press the Apply button first", "Error");
                return;
            }

            SaveAnimation(true);
        }


        

    }


}
