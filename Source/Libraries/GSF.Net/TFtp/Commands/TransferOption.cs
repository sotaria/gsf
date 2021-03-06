﻿//******************************************************************************************************
//  TransferOption.cs - Gbtc
//
//  Copyright © 2020, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may not use this
//  file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  05/12/2020 - J. Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

#region [ Contributor License Agreements ]

//*******************************************************************************************************
//
//   Code based on the following project:
//        https://github.com/Callisto82/tftp.net
//  
//   Copyright © 2011, Michael Baer
//
//*******************************************************************************************************

#endregion

using System;

namespace GSF.Net.TFtp.Commands
{
    /// <summary>
    /// A single transfer options according to RFC2347.
    /// </summary>
    public class TransferOption
    {
        public string Name { get; }
        public string Value { get; set; }
        public bool IsAcknowledged { get; internal set; }

        internal TransferOption(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name must not be null or empty.");

            Name = name;
            Value = value ?? throw new ArgumentNullException(nameof(name));
        }

        public override string ToString() => $"{Name}={Value}";
    }
}
