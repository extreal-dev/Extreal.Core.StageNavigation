using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Extreal.Core.SceneTransition.Test
{
    public class SceneTransitionTest
    {
        private bool _sceneTransitioned;
        private SceneName _currentScene;
        private ISceneTransitioner<SceneName> _sceneTransitioner;

        [UnitySetUp]
        public IEnumerator Initialize()
        {
            var asyncOp = SceneManager.LoadSceneAsync("TestMainScene");
            yield return new WaitUntil(() => asyncOp.isDone);

            var provider = Object.FindObjectOfType<SceneConfigProvider>();
            _sceneTransitioner = new SceneTransitioner<SceneName, UnitySceneName>(provider._sceneConfig);
            _sceneTransitioner.OnSceneTransitioned += this.OnSceneTransitioned;
            this._sceneTransitioned = false;
        }

        [TearDown]
        public void Dispose()
        {
            _sceneTransitioner.OnSceneTransitioned -= this.OnSceneTransitioned;
        }

        [UnityTest]
        public IEnumerator Permanent()
        {
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestPermanent.ToString()).IsValid());

            // Transition to FirstScene without leaving history
            _ = _sceneTransitioner.ReplaceAsync(SceneName.FirstScene);
            yield return WaitUntilSceneChanged();

            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestPermanent.ToString()).IsValid());

            // Transition to SecondScene with leaving history
            _ = _sceneTransitioner.PushAsync(SceneName.SecondScene);
            yield return WaitUntilSceneChanged();

            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestPermanent.ToString()).IsValid());

            // Transition back to FirstScene according to history
            _ = _sceneTransitioner.PopAsync();
            yield return WaitUntilSceneChanged();

            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestPermanent.ToString()).IsValid());

            // Reset transition history
            _sceneTransitioner.Reset();

            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestPermanent.ToString()).IsValid());
        }

        [UnityTest]
        public IEnumerator Replace()
        {
            // Transition to FirstScene without leaving history
            _ = _sceneTransitioner.ReplaceAsync(SceneName.FirstScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.FirstScene, this._currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestFirstStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestFirstModal.ToString()).IsValid());

            // Transition to SecondScene without leaving history
            _ = _sceneTransitioner.ReplaceAsync(SceneName.SecondScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.SecondScene, this._currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestFirstStage.ToString()).IsValid());
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestFirstModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondThirdModal.ToString()).IsValid());

            // Transition to ThirdScene without leaving history
            _ = _sceneTransitioner.ReplaceAsync(SceneName.ThirdScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.ThirdScene, this._currentScene);
            Assert.AreEqual(5, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestThirdStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondThirdModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestThirdModal.ToString()).IsValid());
        }

        [UnityTest]
        public IEnumerator Push()
        {
            // Transition to FirstScene with leaving history
            _ = _sceneTransitioner.PushAsync(SceneName.FirstScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.FirstScene, this._currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestFirstStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestFirstModal.ToString()).IsValid());

            // Transition to SecondScene with leaving history
            _ = _sceneTransitioner.PushAsync(SceneName.SecondScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.SecondScene, this._currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestFirstStage.ToString()).IsValid());
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestFirstModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondThirdModal.ToString()).IsValid());

            // Transition to ThirdScene with leaving history
            _ = _sceneTransitioner.PushAsync(SceneName.ThirdScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.ThirdScene, this._currentScene);
            Assert.AreEqual(5, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestThirdStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondThirdModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestThirdModal.ToString()).IsValid());
        }

        [UnityTest]
        public IEnumerator PopIfNoHistory() => UniTask.ToCoroutine(async () =>
        {
            Exception exception = null;
            try
            {
                await _sceneTransitioner.PopAsync();
            }
            catch (Exception e)
            {
                exception = e;
            }

            Assert.IsNotNull(exception);

            void Test()
            {
                throw exception;
            }

            Assert.That(Test, 
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo("there is no scene transition history"));
        });

        [UnityTest]
        public IEnumerator Pop()
        {
            // Transition to FirstScene with leaving history
            _ = _sceneTransitioner.PushAsync(SceneName.FirstScene);
            yield return WaitUntilSceneChanged();

            // Transition to SecondScene with leaving history
            _ = _sceneTransitioner.PushAsync(SceneName.SecondScene);
            yield return WaitUntilSceneChanged();

            // Transition to ThirdScene with leaving history
            _ = _sceneTransitioner.PushAsync(SceneName.ThirdScene);
            yield return WaitUntilSceneChanged();

            // Transition to FirstScene with leaving history
            _ = _sceneTransitioner.PushAsync(SceneName.FirstScene);
            yield return WaitUntilSceneChanged();

            // Transition back to ThirdScene according to history
            _ = _sceneTransitioner.PopAsync();
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.ThirdScene, this._currentScene);
            Assert.AreEqual(5, SceneManager.sceneCount);
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestThirdStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondThirdModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestThirdModal.ToString()).IsValid());

            // Transition back to SecondScene according to history
            _ = _sceneTransitioner.PopAsync();
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.SecondScene, this._currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestThirdStage.ToString()).IsValid());
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestThirdModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondThirdModal.ToString()).IsValid());

            // Transition back to FirstScene according to history
            _ = _sceneTransitioner.PopAsync();
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.FirstScene, this._currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsFalse(SceneManager.GetSceneByName(UnitySceneName.TestSecondThirdModal.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestFirstStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestFirstModal.ToString()).IsValid());
        }

        [UnityTest]
        public IEnumerator Reset()
        {
            // Transition to FirstScene with leaving history
            _ = _sceneTransitioner.PushAsync(SceneName.FirstScene);
            yield return WaitUntilSceneChanged();

            // Transition to SecondScene with leaving history
            _ = _sceneTransitioner.PushAsync(SceneName.SecondScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.SecondScene, this._currentScene);
            Assert.AreEqual(4, SceneManager.sceneCount);
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondStage.ToString()).IsValid());
            Assert.IsTrue(SceneManager.GetSceneByName(UnitySceneName.TestSecondThirdModal.ToString()).IsValid());

            // Reset transition history
            _sceneTransitioner.Reset();

            // Transition back according to history
            // Nothing is executed
            _ = _sceneTransitioner.PopAsync();
            yield return new WaitForSeconds(1);

            Assert.IsFalse(this._sceneTransitioned);
        }

        [UnityTest]
        public IEnumerator PushReplacePushPop()
        {
            // Initial Transition
            _ = _sceneTransitioner.ReplaceAsync(SceneName.FirstScene);
            yield return WaitUntilSceneChanged();

            // Transition to SecondScene with leaving history
            _ = _sceneTransitioner.PushAsync(SceneName.SecondScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.SecondScene, this._currentScene);

            // Transition to ThirdScene without leaving history
            _ = _sceneTransitioner.PushAsync(SceneName.ThirdScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.ThirdScene, this._currentScene);

            // Transition to FirstScene with leaving history
            _ = _sceneTransitioner.PushAsync(SceneName.FirstScene);
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.FirstScene, this._currentScene);

            // Transition back to Third according to history
            _ = _sceneTransitioner.PopAsync();
            yield return WaitUntilSceneChanged();

            Assert.AreEqual(SceneName.ThirdScene, this._currentScene);
        }

        private IEnumerator WaitUntilSceneChanged()
        {
            yield return new WaitUntil(() => this._sceneTransitioned);
            this._sceneTransitioned = false;
        }

        private void OnSceneTransitioned(SceneName scene)
        {
            this._currentScene = scene;
            this._sceneTransitioned = true;
        }
    }
}
