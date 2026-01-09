using AnimationEditor.MountAnimationCreator;
using Editors.Shared.Core.Common.BaseControl;
using Shared.Core.DevConfig;
using Shared.Core.PackFiles;
using Shared.Core.Settings;
using Shared.Core.ToolCreation;

namespace Editors.AnimationVisualEditors.MountAnimationCreator.DevConfig
{
    internal class MountTool : IDeveloperConfiguration
    {
        private readonly IEditorCreator _editorCreator;
        private readonly IPackFileService _packFileService;

        public MountTool(IEditorCreator editorCreator, IPackFileService packFileService)
        {
            _editorCreator = editorCreator;
            _packFileService = packFileService;
        }


        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.ShowCAWemFiles = true;
        }

        public void OpenFileOnLoad()
        {
            CreateLionAndHu01b(_editorCreator, _packFileService);
        }

        static void CreateLionAndHu01b(IEditorCreator creator, IPackFileService packfileService)
        {
            var riderInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\hef_princess_campaign_01.variantmeshdefinition")
            };

            var mountInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\hef_war_lion.variantmeshdefinition")
            };

            creator.Create(EditorEnums.MountTool_Editor, x => (x as EditorHost<MountAnimationCreatorViewModel>).Editor.SetDebugInputParameters(riderInput, mountInput));
        }
    }
}

  public static class MountAnimationCreator_Debug
  {
      public static void CreateDamselAndGrymgoreEditor(IEditorCreator creator, IEditorDatabase toolFactory, PackFileService packfileService)
      {
        //var editorView = toolFactory.Create("Mount Editor",EditorEnums.MountTool_Editor); //toolFactory.Create<MountAnimationCreatorViewModel>();

        var MainInput = new AnimationToolInput() //var MainInput 
          {
              Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\brt_damsel_campaign_01.variantmeshdefinition")
          };

        var RefInput = new AnimationToolInput()
          {
              Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\lzd_carnosaur_grymloq.variantmeshdefinition")
          };

        //CreateEmptyEditor(editorView);
        creator.Create(EditorEnums.MountTool_Editor, x => (x as EditorHost<MountAnimationCreatorViewModel>).Editor.SetDebugInputParameters(MainInput, RefInput));
      }


      public static void CreateKarlAndSquigEditor(IEditorCreator creator, IEditorDatabase toolFactory, PackFileService packfileService)
      {
          //var editorView = toolFactory.Create<MountAnimationCreatorViewModel>();

          var MainInput = new AnimationToolInput()
          {
              Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\emp_ch_karl.variantmeshdefinition")
          };

          var RefInput = new AnimationToolInput()
          {
              Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\grn_great_cave_squig.variantmeshdefinition")
          };

          //creator.CreateEmptyEditor(editorView);
          creator.Create(EditorEnums.MountTool_Editor, x => (x as EditorHost<MountAnimationCreatorViewModel>).Editor.SetDebugInputParameters(MainInput, RefInput));
      }

      public static void CreateBroodHorrorEditor(IEditorCreator creator, IEditorDatabase toolFactory, PackFileService packfileService)
      {
          //var editorView = toolFactory.Create<MountAnimationCreatorViewModel>();

          var MainInput = new AnimationToolInput()
          {
              Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\skv_plague_priest.variantmeshdefinition")
          };

          var RefInput = new AnimationToolInput()
          {
              Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\skv_brood_horror.variantmeshdefinition")
          };

         creator.Create(EditorEnums.MountTool_Editor, x => (x as EditorHost<MountAnimationCreatorViewModel>).Editor.SetDebugInputParameters(MainInput, RefInput));
    }

      public static void CreateLionAndHu01b(IEditorCreator creator, IEditorDatabase toolFactory, PackFileService packfileService)
      {
          //var editorView = toolFactory.Create<MountAnimationCreatorViewModel>();

          var MainInput = new AnimationToolInput()
          {
              Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\hef_princess_campaign_01.variantmeshdefinition")
          };

          var RefInput = new AnimationToolInput()
          {
              Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\hef_war_lion.variantmeshdefinition")
          };

          creator.Create(EditorEnums.MountTool_Editor, x => (x as EditorHost<MountAnimationCreatorViewModel>).Editor.SetDebugInputParameters(MainInput, RefInput));
    }

      public static void CreateLionAndHu01c(IEditorCreator creator, IEditorDatabase toolFactory, PackFileService packfileService)
      {
          //var editorView = toolFactory.Create<MountAnimationCreatorViewModel>();

          var MainInput = new AnimationToolInput()
          {
              Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\chs_marauder_horsemen.variantmeshdefinition")
          };

          var RefInput = new AnimationToolInput()
          {
              Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\hef_war_lion.variantmeshdefinition")
          };

          creator.Create(EditorEnums.MountTool_Editor, x => (x as EditorHost<MountAnimationCreatorViewModel>).Editor.SetDebugInputParameters(MainInput, RefInput));
    }

      public static void CreateRaptorAndHu01b(IEditorCreator creator, IEditorDatabase toolFactory, PackFileService packfileService)
      {
          //var editorView = toolFactory.Create<MountAnimationCreatorViewModel>();

          var MainInput = new AnimationToolInput()
          {
              Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\hef_princess_campaign_01.variantmeshdefinition")
          };

          var RefInput = new AnimationToolInput()
          {
              Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\def_cold_one.variantmeshdefinition")
          };

          creator.Create(EditorEnums.MountTool_Editor, x => (x as EditorHost<MountAnimationCreatorViewModel>).Editor.SetDebugInputParameters(MainInput, RefInput));
    }

      public static void CreateRaptorAndHu01d(IEditorCreator creator, IEditorDatabase toolFactory, PackFileService packfileService)
      {
          //var editorView = toolFactory.Create<MountAnimationCreatorViewModel>();

          var MainInput = new AnimationToolInput()
          {
              Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\hef_archer_armoured.variantmeshdefinition"),
          };

          var RefInput = new AnimationToolInput()
          {
              Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\def_cold_one.variantmeshdefinition"),
          };

          creator.Create(EditorEnums.MountTool_Editor, x => (x as EditorHost<MountAnimationCreatorViewModel>).Editor.SetDebugInputParameters(MainInput, RefInput));
    }

      public static void CreateRaptorAndHu02(IEditorCreator creator, IEditorDatabase toolFactory, PackFileService packfileService)
      {
          //var editorView = toolFactory.Create<MountAnimationCreatorViewModel>();

          var MainInput = new AnimationToolInput()
          {
              Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\grn_savage_orc_base.variantmeshdefinition"),
          };

          var RefInput = new AnimationToolInput()
          {
              Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\def_cold_one.variantmeshdefinition"),
          };

         creator.Create(EditorEnums.MountTool_Editor, x => (x as EditorHost<MountAnimationCreatorViewModel>).Editor.SetDebugInputParameters(MainInput, RefInput));
    }


      public static void CreateRome2WolfRider(IEditorCreator creator, IEditorDatabase toolFactory, PackFileService packfileService)
      {
          //var editorView = toolFactory.Create<MountAnimationCreatorViewModel>();

          var MainInput = new AnimationToolInput()
          {
              Mesh = packfileService.FindFile(@"variantmeshes\_variantmodels\man\skin\barb_base_full.rigid_model_v2"),
              Animation = packfileService.FindFile(@"animations\rome2\riders\horse_rider\cycles\rider\horse_rider_walk.anim"),
          };

          var RefInput = new AnimationToolInput()
          {
              Mesh = packfileService.FindFile(@"variantmeshes\wh_variantmodels\wf1\grn\grn_giant_wolf\grn_giant_wolf_1.rigid_model_v2"),
              Animation = packfileService.FindFile(@"animations\battle\wolf01\locomotion\wf1_walk_01.anim")
          };

          creator.Create(EditorEnums.MountTool_Editor, x => (x as EditorHost<MountAnimationCreatorViewModel>).Editor.SetDebugInputParameters(MainInput, RefInput));
    }

      public static void CreateRome2WolfRiderAttack(IEditorCreator creator, IEditorDatabase toolFactory, PackFileService packfileService)
      {
          //var editorView = toolFactory.Create<MountAnimationCreatorViewModel>();

          var MainInput = new AnimationToolInput()
          {
              Mesh = packfileService.FindFile(@"variantmeshes\_variantmodels\man\skin\barb_base_full.rigid_model_v2"),
              Animation = packfileService.FindFile(@"animations\rome2\riders\horse_rider\attack\rider\sws_rider_attack_01.anim"),
          };

          var RefInput = new AnimationToolInput()
          {
              Mesh = packfileService.FindFile(@"variantmeshes\wh_variantmodels\wf1\grn\grn_giant_wolf\grn_giant_wolf_1.rigid_model_v2"),
              Animation = packfileService.FindFile(@"animations\battle\wolf01\attacks\wf1_attack_01.anim")
          };

          creator.Create(EditorEnums.MountTool_Editor, x => (x as EditorHost<MountAnimationCreatorViewModel>).Editor.SetDebugInputParameters(MainInput, RefInput));
    }


  }
