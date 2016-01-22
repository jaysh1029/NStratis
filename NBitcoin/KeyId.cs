﻿using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public interface IScriptTxDestination : IDestination
	{
		/// <summary>
		/// Extract the redeem script from the input if it matches this TxDestination
		/// </summary>
		/// <param name="txIn">The input</param>
		/// <returns>The redeem if match, else null</returns>
		Script ExtractRedeemScript(IndexedTxIn txIn);
		/// <summary>
		/// Create a IScriptCoin from a normal coin and the redeem
		/// </summary>
		/// <param name="coin">The Coin</param>
		/// <param name="redeem">The redeem</param>
		/// <returns>The ScriptCoin if the redeem match this TxDestination, else null</returns>
		IScriptCoin ToScriptCoin(Coin coin, Script redeem);
	}
	public abstract class TxDestination : IDestination
	{
		internal byte[] _DestBytes;

		public TxDestination()
		{
			_DestBytes = new byte[] { 0 };
		}

		public TxDestination(byte[] value)
		{
			if(value == null)
				throw new ArgumentNullException("value");
			_DestBytes = value;
		}		

		public TxDestination(string value)
		{
			_DestBytes = Encoders.Hex.DecodeData(value);
			_Str = value;
		}

		public abstract BitcoinAddress GetAddress(Network network);

		#region IDestination Members

		public abstract Script ScriptPubKey
		{
			get;
		}

		#endregion


		public byte[] ToBytes()
		{
			return ToBytes(false);
		}
		public byte[] ToBytes(bool @unsafe)
		{
			if(@unsafe)
				return _DestBytes;
			var array = new byte[_DestBytes.Length];
			Array.Copy(_DestBytes, array, _DestBytes.Length);
			return array;
		}

		public override bool Equals(object obj)
		{
			TxDestination item = obj as TxDestination;
			if(item == null)
				return false;
			return Utils.ArrayEqual(_DestBytes, item._DestBytes) && item.GetType() == this.GetType();
		}
		public static bool operator ==(TxDestination a, TxDestination b)
		{
			if(System.Object.ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return Utils.ArrayEqual(a._DestBytes, b._DestBytes) && a.GetType() == b.GetType();
		}

		public static bool operator !=(TxDestination a, TxDestination b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return Utils.GetHashCode(_DestBytes);
		}

		string _Str;
		public override string ToString()
		{
			if(_Str == null)
				_Str = Encoders.Hex.EncodeData(_DestBytes);
			return _Str;
		}
	}
	public class KeyId : TxDestination
	{
		public KeyId()
			: this(0)
		{

		}

		public KeyId(byte[] value)
			: base(value)
		{
			if(value.Length != 20)
				throw new ArgumentException("value should be 20 bytes", "value");
		}
		public KeyId(uint160 value)
			: base(value.ToBytes())
		{

		}

		public KeyId(string value)
			: base(value)
		{
		}

		public override Script ScriptPubKey
		{
			get
			{
				return PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(this);
			}
		}

		public override BitcoinAddress GetAddress(Network network)
		{
			return new BitcoinPubKeyAddress(this, network);
		}
	}
	public class WitKeyId : TxDestination
	{
		public WitKeyId()
			: this(0)
		{

		}

		public WitKeyId(byte[] value)
			: base(value)
		{
			if(value.Length != 20)
				throw new ArgumentException("value should be 20 bytes", "value");
		}
		public WitKeyId(uint160 value)
			: base(value.ToBytes())
		{

		}

		public WitKeyId(string value)
			: base(value)
		{
		}

		public WitKeyId(KeyId keyId)
			:base(keyId.ToBytes())
		{

		}


		public override Script ScriptPubKey
		{
			get
			{
				return PayToWitTemplate.Instance.GenerateScriptPubKey(OpcodeType.OP_0, _DestBytes);
			}
		}

		public Script WitScriptPubKey
		{
			get
			{
				return new KeyId(_DestBytes).ScriptPubKey;
			}
		}

		public override BitcoinAddress GetAddress(Network network)
		{
			return new BitcoinWitPubKeyAddress(this, network);
		}
	}

	public class WitScriptId : TxDestination, IScriptTxDestination
	{
		public WitScriptId()
			: this(0)
		{

		}

		public WitScriptId(byte[] value)
			: base(value)
		{
			if(value.Length != 32)
				throw new ArgumentException("value should be 32 bytes", "value");
		}
		public WitScriptId(uint256 value)
			: base(value.ToBytes())
		{

		}

		public WitScriptId(string value)
			: base(value)
		{
		}

		public WitScriptId(Script script)
			: this(Hashes.Hash256(script._Script))
		{
		}

		public override Script ScriptPubKey
		{
			get
			{
				return PayToWitTemplate.Instance.GenerateScriptPubKey(OpcodeType.OP_0, _DestBytes);
			}
		}

		public override BitcoinAddress GetAddress(Network network)
		{
			return new BitcoinWitScriptAddress(this, network);
		}

		public Script ExtractRedeemScript(IndexedTxIn txIn)
		{
			var lastOp = txIn.WitScript.Pushes.LastOrDefault();
			return lastOp == null ? null : Script.FromBytesUnsafe(lastOp);
		}

		public IScriptCoin ToScriptCoin(Coin coin, Script redeem)
		{
			if(coin == null)
				throw new ArgumentNullException("coin");
			if(redeem == null)
				throw new ArgumentNullException("redeem");
			if(redeem.WitHash != this)
				return null;
			return coin.ToWitScriptCoin(redeem);
		}
	}

	public class ScriptId : TxDestination, IScriptTxDestination
	{
		public ScriptId()
			: this(0)
		{

		}

		public ScriptId(byte[] value)
			: base(value)
		{
			if(value.Length != 20)
				throw new ArgumentException("value should be 20 bytes", "value");
		}
		public ScriptId(uint160 value)
			: base(value.ToBytes())
		{

		}

		public ScriptId(string value)
			: base(value)
		{
		}

		public ScriptId(Script script)
			: this(Hashes.Hash160(script._Script))
		{
		}

		public override Script ScriptPubKey
		{
			get
			{
				return PayToScriptHashTemplate.Instance.GenerateScriptPubKey(this);
			}
		}

		public override BitcoinAddress GetAddress(Network network)
		{
			return new BitcoinScriptAddress(this, network);
		}

		#region IScriptTxDestination Members

		public Script ExtractRedeemScript(IndexedTxIn txIn)
		{
			var lastOp = txIn.ScriptSig.ToOps().LastOrDefault();
			return lastOp == null ? null : Script.FromBytesUnsafe(lastOp.PushData);
		}

		public IScriptCoin ToScriptCoin(Coin coin, Script redeem)
		{
			if(coin == null)
				throw new ArgumentNullException("coin");
			if(redeem == null)
				throw new ArgumentNullException("redeem");
			if(redeem.Hash != this)
				return null;
			return coin.ToScriptCoin(redeem);
		}

		#endregion
	}
}
