﻿/*
 *	This file is part of SharpMapMapViewer
 *  SharpMapMapViewer is free software © 2008 Newgrove Consultants Limited, 
 *  http://www.newgrove.com; you can redistribute it and/or modify it under the terms 
 *  of the current GNU Lesser General Public License (LGPL) as published by and 
 *  available from the Free Software Foundation, Inc., 
 *  59 Temple Place, Suite 330, Boston, MA 02111-1307 USA: http://fsf.org/    
 *  This program is distributed without any warranty; 
 *  without even the implied warranty of merchantability or fitness for purpose.  
 *  See the GNU Lesser General Public License for the full details. 
 *  
 *  Author: John Diss 2008
 * 
 */
using System;

#if DOTNET35

#endif

namespace MapViewer.Commands
{
    public class Command<TEventArgs>
        : Command, ICommand<TEventArgs>
        where TEventArgs : CommandEventArgs
    {
        public Command(string commandName)
            : base(commandName)
        {
        }

        #region ICommand<TEventArgs> Members

        public new event EventHandler<TEventArgs> CommandFired;

        public void FireCommand(TEventArgs args)
        {
            FireCommand();
            if (CommandFired != null)
                CommandFired(this, args);
        }

        #endregion
    }
}