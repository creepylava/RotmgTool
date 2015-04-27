using System;
using System.Collections.Generic;
using RotmgTool.SWF;

namespace RotmgTool.Proxy
{
	internal class SwfPatcher
	{
		private readonly string targetHost;
		private readonly string targetHostWithPort;
		private readonly IToolInstance tool;

		public SwfPatcher(IToolInstance tool, string proxyHost, int proxyPort)
		{
			this.tool = tool;
			targetHost = proxyHost;
			targetHostWithPort = proxyPort == 80 ? proxyHost : proxyHost + ":" + proxyPort;
		}

		public void Patch(long ts, ref byte[] swf, bool isLoader)
		{
			var swfFile = new SwfFile(swf);
			string version = null;

			foreach (var tag in swfFile.Tags)
				if (tag.Type == 82)
				{
					byte[] cnt = tag.Content;

					uint pos = 4;
					while (cnt[pos] != 0) pos++;
					ABCFile file = new ABCReader(cnt, ++pos).abc;

					if (isLoader)
					{
						for (int i = 0; i < file.strings.Length; i++)
						{
							if (file.strings[i] == "www.realmofthemadgod.com")
								file.strings[i] = targetHostWithPort;
							else if (file.strings[i] == "realmofthemadgodhrd.appspot.com")
								file.strings[i] = targetHostWithPort;
							else if (file.strings[i] == "rotmg_loader_port")
							{
								// When use AGCLoader directly, rotmg_loader_port not set in flashvars,
								// so port number must be appended.
								// However when use homepage, rotmg_loader_port is set,
								// so port number must not be appended.
								// To simpify the cases, patch the reading of that variable.
								file.strings[i] = "x";
							}
						}
					}
					else
					{
						int stringRef = 0;
						for (int i = 0; i < file.strings.Length; i++)
						{
							if (file.strings[i] == "www.realmofthemadgod.com")
							{
								file.strings[i] = targetHost;
								stringRef = i;
							}
							else if (file.strings[i] == "realmofthemadgodhrd.appspot.com")
								file.strings[i] = targetHostWithPort;
							else if (file.strings[i] == "xlate.kabam.com")
								// Not necessary, but prevent the background 'Play at' text from appearing.
								file.strings[i] = targetHost;
							else if (file.strings[i] == "https://")
								file.strings[i] = "http://";
						}
						for (int i = 0; i < file.classes.Length; i++)
							for (int j = 0; j < file.classes[i].traits.Length; j++)
							{
								var trait = file.classes[i].traits[j];
								if (!(trait.kind == TraitKind.Const && trait.Slot.vkind == ASType.Utf8 && trait.Slot.vindex == stringRef))
									continue;

								// patch string decrypter
								var cinit = file.classes[i].cinit;
								for (int k = 0; k < file.bodies.Length; k++)
								{
									var body = file.bodies[k];
									if (body.method != cinit) continue;

									for (int l = 0; l < body.instructions.Length; l++) // ugh. ijkl, so many loops...
									{
										var instr = body.instructions[l];
										if (instr.opcode != Opcode.OP_initproperty || instr.arguments[0].index != trait.name)
											continue;

										instr.opcode = Opcode.OP_pop;
										instr.arguments = new ABCFile.Instruction.Argument[0];
										var newInstrs = new List<ABCFile.Instruction>(body.instructions);
										newInstrs[l] = instr;
										newInstrs.Insert(l, instr);
										// fix branches
										for (int m = 0; m < newInstrs.Count; m++)
										{
											var opcodeInfo = OpcodeInfo.opcodeDict[newInstrs[m].opcode];
											for (int n = 0; n < opcodeInfo.argumentTypes.Length; n++) // ...and a few more =P
												switch (opcodeInfo.argumentTypes[n])
												{
													case OpcodeArgumentType.JumpTarget:
													case OpcodeArgumentType.SwitchDefaultTarget:
														if (newInstrs[m].arguments[n].jumpTarget.index > l)
															newInstrs[m].arguments[n].jumpTarget.index++;
														break;
													case OpcodeArgumentType.SwitchTargets:
														for (int o = 0; o < newInstrs[m].arguments[n].switchTargets.Length; o++)
															if (newInstrs[m].arguments[n].switchTargets[o].index > l)
																newInstrs[m].arguments[n].switchTargets[o].index++;
														break;
													default:
														break;
												}
										}
										body.instructions = newInstrs.ToArray();
										break;
									}
									file.bodies[k] = body;
									break;
								}
								break;
							}

						version = SWFAnalyzer.AnalyzePackets(tool, ts, file);
					}

					var newAbc = new ABCWriter(file).buf;
					var newTag = new byte[newAbc.Length + pos];
					Buffer.BlockCopy(cnt, 0, newTag, 0, (int)pos);
					Buffer.BlockCopy(newAbc, 0, newTag, (int)pos, newAbc.Length);

					tag.Content = newTag;
				}

			if (version != null)
				SWFAnalyzer.AnalyzeXML(tool, version, swfFile);

			var newSwf = swfFile.Write();
			swf = newSwf;
		}
	}
}