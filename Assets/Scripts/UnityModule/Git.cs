﻿using System.Collections.Generic;
using System.Text;
using UniRx;
using UnityModule.Settings;

namespace UnityModule {

    public static class Git {

        private enum SubCommandType {
            Add,
            Branch,
            Checkout,
            Commit,
            Push,
            RevParse,
            Rm,
        }

        private static readonly Dictionary<SubCommandType, string> SUB_COMMAND_MAP = new Dictionary<SubCommandType, string>() {
            { SubCommandType.Add     , "add" },
            { SubCommandType.Branch  , "branch" },
            { SubCommandType.Checkout, "checkout" },
            { SubCommandType.Commit  , "commit" },
            { SubCommandType.Push    , "push" },
            { SubCommandType.RevParse, "rev-parse" },
            { SubCommandType.Rm      , "rm" },
        };

        public static IObservable<string> Add(IEnumerable<string> files = null, List<string> argumentList = null) {
            argumentList = CreateListIfNull(argumentList);
            if (files == null) {
                // ファイル未指定の場合全ファイルを追加する
                argumentList.Add(".");
            } else {
                argumentList.Add(string.Format("-- {0}", files.Combine()));
            }
            return Run(SubCommandType.Add, argumentList);
        }

        public static IObservable<string> Branch(string branchName, bool force = false, List<string> argumentMap = null) {
            argumentMap = CreateListIfNull(argumentMap);
            if (force) {
                argumentMap.Add("-f");
            }
            argumentMap.Add(branchName);
            return Run(SubCommandType.Branch, argumentMap);
        }

        public static IObservable<string> Checkout(string branchName, bool create = false, bool force = false, List<string> argumentMap = null) {
            argumentMap = CreateListIfNull(argumentMap);
            if (create) {
                Branch(branchName, force);
            }
            argumentMap.Add(branchName);
            return Run(SubCommandType.Branch, argumentMap);
        }

        public static IObservable<string> Commit(string message, List<string> argumentList = null) {
            argumentList = CreateListIfNull(argumentList);
            // コマンド経由の場合何らかのメッセージを入れないとコミットできない
            argumentList.Add(string.Format("-m {0}", message.Quot()));
            return Run(SubCommandType.Commit, argumentList);
        }

        public static IObservable<string> Push(string branchName, string remoteName = "origin", List<string> argumentMap = null) {
            argumentMap = CreateListIfNull(argumentMap);
            argumentMap.Add(remoteName);
            argumentMap.Add(branchName);
            return Run(SubCommandType.Commit, argumentMap);
        }

        public static IObservable<string> RevParse(List<string> argumentList = null) {
            argumentList = CreateListIfNull(argumentList);
            return Run(SubCommandType.RevParse, argumentList);
        }

        public static IObservable<string> Rm(IEnumerable<string> files, bool ignoreUnmatch = true, List<string> argumentList = null) {
            argumentList = CreateListIfNull(argumentList);
            argumentList.Add(string.Format("-- {0}", files.Combine()));
            if (ignoreUnmatch) {
                argumentList.Add("--ignore-unmatch");
            }
            return Run(SubCommandType.Rm, argumentList);
        }

        public static IObservable<string> GetCurrentCommitHash() {
            return RevParse(
                new List<string>() {
                    "HEAD",
                }
            );
        }

        private static IObservable<string> Run(SubCommandType subCommandType, List<string> argumentMap = null) {
            return Observable
                .Create<string>(
                    (observer) => {
                        System.Diagnostics.Process process = new System.Diagnostics.Process {
                            StartInfo = {
                                FileName = EnvironmentSetting.Instance.Path.CommandGit,
                                Arguments = string.Format("{0}{1}", SUB_COMMAND_MAP[subCommandType], CreateArgument(argumentMap)),
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                CreateNoWindow = true
                            },
                            EnableRaisingEvents = true,
                        };
                        process.Exited += (sender, args) => {
                            System.Diagnostics.Process p = (System.Diagnostics.Process)sender;
                            if (p.ExitCode == 0) {
                                observer.OnNext(process.StandardOutput.ReadToEnd());
                                observer.OnCompleted();
                            } else {
                                observer.OnError(new System.InvalidOperationException(process.StandardError.ReadToEnd()));
                            }
                            p.Close();
                        };
                        process.Start();
                        return null;
                    }
                );
        }

        private static List<string> CreateListIfNull(List<string> argumentList) {
            if (argumentList == default(List<string>)) {
                argumentList = new List<string>();
            }
            return argumentList;
        }

        private static string CreateArgument(List<string> argumentList) {
            if (argumentList == default(List<string>) || argumentList.Count == 0) {
                return string.Empty;
            }
            StringBuilder sb = new StringBuilder();
            foreach (string argument in argumentList) {
                sb.AppendFormat(" {0}", argument);
            }
            return sb.ToString();
        }

        private static string Combine(this IEnumerable<string> items, bool surroundDoubleQuatation = true) {
            StringBuilder sb = new StringBuilder();
            foreach (string item in items) {
                sb.AppendFormat(
                    "{1}{0}",
                    surroundDoubleQuatation ? item.Quot() : item,
                    sb.Length > 0 ? " " : string.Empty
                );
            }
            return sb.ToString();
        }

        private static string Quot(this string original) {
            return string.Format("{1}{0}{1}", original, "\"");
        }

    }

}