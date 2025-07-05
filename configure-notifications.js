#!/usr/bin/env node

/**
 * Notification Provider Configuration Script
 * 
 * This script helps you quickly switch between different notification provider configurations
 * for testing Firebase, SignalR, or both.
 */

const fs = require('fs');
const path = require('path');

const FRONTEND_ENV_FILE = path.join(__dirname, 'yapplr-frontend', '.env.local');
const BACKEND_CONFIG_FILE = path.join(__dirname, 'Yapplr.Api', 'appsettings.Development.json');

const configurations = {
  'signalr-only': {
    name: 'SignalR Only (Frontend)',
    description: 'Frontend uses SignalR only, API keeps Firebase for mobile',
    frontend: {
      NEXT_PUBLIC_ENABLE_SIGNALR: 'true'
    },
    backend: {
      'NotificationProviders:Firebase:Enabled': true,
      'NotificationProviders:SignalR:Enabled': true
    }
  },
  'disable-signalr': {
    name: 'Disable SignalR (Polling Only)',
    description: 'Disable SignalR for frontend (polling only), API keeps Firebase for mobile',
    frontend: {
      NEXT_PUBLIC_ENABLE_SIGNALR: 'false'
    },
    backend: {
      'NotificationProviders:Firebase:Enabled': true,
      'NotificationProviders:SignalR:Enabled': false
    }
  }
};

function updateFrontendEnv(config) {
  if (!fs.existsSync(FRONTEND_ENV_FILE)) {
    console.error('Frontend .env.local file not found:', FRONTEND_ENV_FILE);
    return false;
  }

  let envContent = fs.readFileSync(FRONTEND_ENV_FILE, 'utf8');
  
  // Update or add each configuration value
  Object.entries(config.frontend).forEach(([key, value]) => {
    const regex = new RegExp(`^${key}=.*$`, 'm');
    if (regex.test(envContent)) {
      envContent = envContent.replace(regex, `${key}=${value}`);
    } else {
      envContent += `\n${key}=${value}`;
    }
  });

  fs.writeFileSync(FRONTEND_ENV_FILE, envContent);
  console.log('✅ Updated frontend configuration');
  return true;
}

function updateBackendConfig(config) {
  if (!fs.existsSync(BACKEND_CONFIG_FILE)) {
    console.error('Backend config file not found:', BACKEND_CONFIG_FILE);
    return false;
  }

  try {
    const configContent = JSON.parse(fs.readFileSync(BACKEND_CONFIG_FILE, 'utf8'));
    
    // Update notification provider settings
    if (!configContent.NotificationProviders) {
      configContent.NotificationProviders = {
        Firebase: { Enabled: true },
        SignalR: { Enabled: true }
      };
    }

    // Apply configuration changes
    Object.entries(config.backend).forEach(([key, value]) => {
      const parts = key.split(':');
      if (parts.length === 3 && parts[0] === 'NotificationProviders') {
        const provider = parts[1];
        const property = parts[2];
        
        if (!configContent.NotificationProviders[provider]) {
          configContent.NotificationProviders[provider] = {};
        }
        
        configContent.NotificationProviders[provider][property] = value;
      }
    });

    fs.writeFileSync(BACKEND_CONFIG_FILE, JSON.stringify(configContent, null, 2));
    console.log('✅ Updated backend configuration');
    return true;
  } catch (error) {
    console.error('Error updating backend config:', error.message);
    return false;
  }
}

function showCurrentConfig() {
  console.log('\n📋 Current Configuration:');
  
  // Show frontend config
  if (fs.existsSync(FRONTEND_ENV_FILE)) {
    const envContent = fs.readFileSync(FRONTEND_ENV_FILE, 'utf8');
    const firebaseEnabled = envContent.match(/NEXT_PUBLIC_ENABLE_FIREBASE=(.+)/)?.[1] || 'not set';
    const signalrEnabled = envContent.match(/NEXT_PUBLIC_ENABLE_SIGNALR=(.+)/)?.[1] || 'not set';
    
    console.log('  Frontend:');
    console.log(`    Firebase: ${firebaseEnabled}`);
    console.log(`    SignalR: ${signalrEnabled}`);
  }

  // Show backend config
  if (fs.existsSync(BACKEND_CONFIG_FILE)) {
    try {
      const configContent = JSON.parse(fs.readFileSync(BACKEND_CONFIG_FILE, 'utf8'));
      const providers = configContent.NotificationProviders || {};
      
      console.log('  Backend:');
      console.log(`    Firebase: ${providers.Firebase?.Enabled ?? 'not set'}`);
      console.log(`    SignalR: ${providers.SignalR?.Enabled ?? 'not set'}`);
    } catch (error) {
      console.log('  Backend: Error reading config');
    }
  }
  console.log('');
}

function showHelp() {
  console.log('🔔 Notification Provider Configuration Tool\n');
  console.log('Usage: node configure-notifications.js [configuration]\n');
  console.log('Available configurations:');
  
  Object.entries(configurations).forEach(([key, config]) => {
    console.log(`  ${key.padEnd(15)} - ${config.name}`);
    console.log(`  ${' '.repeat(15)}   ${config.description}`);
  });
  
  console.log('\nExamples:');
  console.log('  node configure-notifications.js firebase-only');
  console.log('  node configure-notifications.js signalr-only');
  console.log('  node configure-notifications.js both');
  console.log('  node configure-notifications.js status');
  console.log('');
}

function main() {
  const args = process.argv.slice(2);
  const command = args[0];

  if (!command || command === 'help' || command === '--help' || command === '-h') {
    showHelp();
    return;
  }

  if (command === 'status') {
    showCurrentConfig();
    return;
  }

  const config = configurations[command];
  if (!config) {
    console.error(`❌ Unknown configuration: ${command}`);
    console.log('Run with --help to see available configurations');
    process.exit(1);
  }

  console.log(`🔧 Applying configuration: ${config.name}`);
  console.log(`   ${config.description}\n`);

  const frontendSuccess = updateFrontendEnv(config);
  const backendSuccess = updateBackendConfig(config);

  if (frontendSuccess && backendSuccess) {
    console.log('\n✅ Configuration applied successfully!');
    console.log('\n📝 Next steps:');
    console.log('   1. Restart the backend API server');
    console.log('   2. Restart the frontend development server');
    console.log('   3. Visit /notification-test to verify the configuration');
    console.log('');
  } else {
    console.log('\n❌ Configuration failed. Please check the errors above.');
    process.exit(1);
  }
}

if (require.main === module) {
  main();
}
