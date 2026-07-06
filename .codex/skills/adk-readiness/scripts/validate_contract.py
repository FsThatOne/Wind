#!/usr/bin/env python3
"""
Validate evaluator result against the common contract.

Checks that _evaluator_result.json has required fields and conforms to the schema.

Usage:
    python3 validate_contract.py --result <result.json>
    python3 validate_contract.py --dir <output_dir>
"""

import argparse
import json
import os
import sys


REQUIRED_FIELDS = ['evaluator', 'mode', 'reportPath']
OPTIONAL_FIELDS = ['version', 'timestamp', 'score', 'level', 'summary', 'dataPath']
ALL_FIELDS = REQUIRED_FIELDS + OPTIONAL_FIELDS

VALID_MODES = ['quick', 'deep']


def validate_result(result, output_dir=None):
    """Validate an evaluator result dict.

    Returns:
        tuple: (is_valid, list of errors, list of warnings)
    """
    errors = []
    warnings = []

    # Check required fields
    for field in REQUIRED_FIELDS:
        if field not in result:
            errors.append(f'Missing required field: {field}')
        elif not result[field]:
            errors.append(f'Required field is empty: {field}')

    # Check mode validity
    if 'mode' in result and result['mode'] not in VALID_MODES:
        errors.append(f"Invalid mode: {result['mode']}. Must be one of: {VALID_MODES}")

    # Check score range
    if 'score' in result and result['score'] is not None:
        try:
            score = float(result['score'])
            if score < 0 or score > 100:
                warnings.append(f'Score out of 0-100 range: {score}')
        except (TypeError, ValueError):
            errors.append(f'Invalid score type: {type(result["score"]).__name__}')

    # Check that reportPath exists if output_dir is provided
    if output_dir and 'reportPath' in result and result['reportPath']:
        report_full = os.path.join(output_dir, result['reportPath'])
        if not os.path.isfile(report_full):
            errors.append(f'Report file not found: {report_full}')

    # Check for unknown fields (info only, not error)
    unknown_fields = [k for k in result.keys() if k not in ALL_FIELDS]
    if unknown_fields:
        warnings.append(f'Unknown fields (allowed but not standard): {unknown_fields}')

    is_valid = len(errors) == 0
    return is_valid, errors, warnings


def validate_directory(output_dir):
    """Validate a complete evaluator output directory."""
    errors = []
    warnings = []

    result_path = os.path.join(output_dir, '_evaluator_result.json')
    if not os.path.isfile(result_path):
        errors.append(f'Missing _evaluator_result.json in {output_dir}')
        return False, errors, warnings

    try:
        with open(result_path, 'r') as f:
            result = json.load(f)
    except json.JSONDecodeError as e:
        errors.append(f'Invalid JSON in _evaluator_result.json: {e}')
        return False, errors, warnings

    is_valid, val_errors, val_warnings = validate_result(result, output_dir)
    errors.extend(val_errors)
    warnings.extend(val_warnings)

    return is_valid, errors, warnings


def main():
    parser = argparse.ArgumentParser(description='Validate evaluator contract')
    parser.add_argument('--result', help='Path to _evaluator_result.json')
    parser.add_argument('--dir', help='Path to evaluator output directory')

    args = parser.parse_args()

    if not args.result and not args.dir:
        print('Error: must specify --result or --dir', file=sys.stderr)
        sys.exit(1)

    if args.dir:
        is_valid, errors, warnings = validate_directory(args.dir)
    else:
        try:
            with open(args.result, 'r') as f:
                result = json.load(f)
            is_valid, errors, warnings = validate_result(result)
        except json.JSONDecodeError as e:
            print(f'Error: invalid JSON: {e}', file=sys.stderr)
            sys.exit(1)

    if errors:
        print('ERRORS:')
        for e in errors:
            print(f'  - {e}')

    if warnings:
        print('\nWARNINGS:')
        for w in warnings:
            print(f'  - {w}')

    if is_valid:
        print('\nPASSED: Contract validation passed')
    else:
        print('\nFAILED: Contract validation failed')
        sys.exit(1)


if __name__ == '__main__':
    main()
