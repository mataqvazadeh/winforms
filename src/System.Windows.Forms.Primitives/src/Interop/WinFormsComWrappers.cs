﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.InteropServices;
using Windows.Win32.System.Com;

internal partial class Interop
{
    /// <summary>
    ///  The ComWrappers implementation for System.Windows.Forms.Primitive's COM interop usages.
    /// </summary>
    internal unsafe partial class WinFormsComWrappers : ComWrappers
    {
        private const int S_OK = 0;

        internal static WinFormsComWrappers Instance { get; } = new WinFormsComWrappers();

        private WinFormsComWrappers() { }

        internal static void PopulateIUnknownVTable(IUnknown.Vtbl* unknown)
        {
            GetIUnknownImpl(out nint fpQueryInterface, out nint fpAddRef, out nint fpRelease);
            unknown->QueryInterface_1 = (delegate* unmanaged[Stdcall]<IUnknown*, Guid*, void**, HRESULT>)fpQueryInterface;
            unknown->AddRef_2 = (delegate* unmanaged[Stdcall]<IUnknown*, uint>)fpAddRef;
            unknown->Release_3 = (delegate* unmanaged[Stdcall]<IUnknown*, uint>)fpRelease;
        }

        protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
        {
            if (obj is not IManagedWrapper vtables)
            {
                Debug.Fail("object does not implement IManagedWrapper");
                count = 0;
                return null;
            }

            ComInterfaceTable table = vtables.GetComInterfaceTable();
            count = table.Count;

            return table.Entries;
        }

        protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
        {
            Debug.Assert(flags == CreateObjectFlags.UniqueInstance
                || flags == CreateObjectFlags.None
                || flags == CreateObjectFlags.Unwrap);

            int hr = Marshal.QueryInterface(externalComObject, in IID.GetRef<IErrorInfo>(), out IntPtr errorInfoComObject);
            if (hr == S_OK)
            {
                Marshal.Release(externalComObject);
                return new ErrorInfoWrapper(errorInfoComObject);
            }

            throw new NotImplementedException();
        }

        protected override void ReleaseObjects(IEnumerable objects)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///  For the given <paramref name="this"/> pointer unwrap the associated managed object and use it to
        ///  invoke <paramref name="func"/>.
        /// </summary>
        /// <remarks>
        ///  <para>
        ///   Handles exceptions and converts to <see cref="HRESULT"/>.
        ///  </para>
        /// </remarks>
        internal static HRESULT UnwrapAndInvoke<TThis, TInterface>(TThis* @this, Func<TInterface, HRESULT> func)
            where TThis : unmanaged
            where TInterface : class
        {
            try
            {
                TInterface? @object = ComInterfaceDispatch.GetInstance<TInterface>((ComInterfaceDispatch*)@this);
                return @object is null ? HRESULT.COR_E_OBJECTDISPOSED : func(@object);
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        /// <summary>
        ///  For the given <paramref name="this"/> pointer unwrap the associated managed object and use it to
        ///  invoke <paramref name="action"/>.
        /// </summary>
        /// <remarks>
        ///  <para>
        ///   Handles exceptions and converts to <see cref="HRESULT"/>.
        ///  </para>
        /// </remarks>
        internal static HRESULT UnwrapAndInvoke<TThis, TInterface>(TThis* @this, Action<TInterface> action)
            where TThis : unmanaged
            where TInterface : class
        {
            try
            {
                TInterface? @object = ComInterfaceDispatch.GetInstance<TInterface>((ComInterfaceDispatch*)@this);
                if (@object is null)
                {
                    return HRESULT.COR_E_OBJECTDISPOSED;
                }

                action(@object);
                return HRESULT.S_OK;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}
